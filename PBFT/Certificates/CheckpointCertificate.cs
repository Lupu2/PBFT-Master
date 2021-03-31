using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Certificates
{
    public class CheckpointCertificate : IQCertificate, IPersistable
    {
        public int LastSeqNr {get; set;}
        public byte[] StateDigest {get; set;}
        public bool Stable {get; set;}
        public CList<Checkpoint> ProofList {get; set;}

        //public Source<CheckpointCertificate> CheckpointBridge {get; set;}

        public Action<CheckpointCertificate> EmitCheckpoint;

        public CheckpointCertificate(int seqLimit, byte[] digest, Action<CheckpointCertificate> emitCheckpoint)
        {
            LastSeqNr = seqLimit;
            StateDigest = digest;
            Stable = false;
            ProofList = new CList<Checkpoint>();
            //CheckpointBridge = check;
            EmitCheckpoint = emitCheckpoint;
        }
        
        [JsonConstructor]
        public CheckpointCertificate(int seqLimit, byte[] digest, bool stable, Action<CheckpointCertificate> emitCheckpoint, CList<Checkpoint> proofs)
        {
            LastSeqNr = seqLimit;
            StateDigest = digest;
            Stable = stable;
            ProofList = proofs;
            //CheckpointBridge = check;
            EmitCheckpoint = emitCheckpoint;
        }
        
        public bool QReached(int nodes) => (ProofList.Count-AccountForDuplicates()) >= 2 * nodes + 1;
        private int AccountForDuplicates()
        {
            //Source: https://stackoverflow.com/questions/53512523/count-of-duplicate-items-in-a-c-sharp-list/53512576
            if (ProofList.Count < 2) return 0;
            var count = ProofList//there is an issue here
                .GroupBy(c => new {c.ServID, c.Signature})
                .Where(c => c.Count() > 1)
                .Sum(c => c.Count()-1);
            return count;
        }

        public bool ProofsAreValid()
        {
            if (ProofList.Count < 1) return false;
            foreach (var check in ProofList)
            {
                if (check.StableSeqNr != LastSeqNr) return false;
                if (check.StateDigest == null && StateDigest != null || check.StateDigest != null && StateDigest == null) 
                    return false;
                if (check.StateDigest != null && StateDigest != null && !check.StateDigest.SequenceEqual(StateDigest)) 
                    return false;
                if (check.Signature == null) return false;
            }
            return true;
        }

        public bool ValidateCertificate(int nodes)
        {
            if (QReached(nodes) && ProofsAreValid()) Stable = true;
            return Stable;
        }

        public void ResetCertificate()
        {
            Stable = false;
            ProofList = new CList<Checkpoint>();
        }

        public void AppendProof(Checkpoint check, RSAParameters pubkey, int failureNr)
        {
            //validate message
            //validate certificate
            //if certificate becomes stable call emit CheckpointCertificate
            if (check.Validate(pubkey) && check.StableSeqNr == LastSeqNr && check.StateDigest.SequenceEqual(StateDigest)) 
                ProofList.Add(check);
            Stable = ValidateCertificate(failureNr);
            if (Stable) EmitCertificate();
        }

        public bool CompareAndValidate(CheckpointCertificate ccert)
        {
            if (!ccert.Stable) return false;
            if (!StateDigest.SequenceEqual(ccert.StateDigest)) return false;
            return true;
        }

        private void EmitCertificate()
        {
            Console.WriteLine("calling callback function");
            //CheckpointBridge.Emit(this);
            EmitCheckpoint(this);
        }


        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(LastSeqNr), LastSeqNr);
            stateToSerialize.Set(nameof(StateDigest), Serializer.SerializeHash(StateDigest));
            stateToSerialize.Set(nameof(Stable), Stable);
            stateToSerialize.Set(nameof(EmitCheckpoint), EmitCheckpoint);
            stateToSerialize.Set(nameof(ProofList), ProofList);
            //stateToSerialize.Set(nameof(CheckpointBridge), CheckpointBridge);
        }
        
        private static CheckpointCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CheckpointCertificate(
                sd.Get<int>(nameof(LastSeqNr)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(StateDigest))),
                sd.Get<bool>(nameof(Stable)),
                sd.Get<Action<CheckpointCertificate>>(nameof(EmitCheckpoint)),
                //sd.Get<Source<CheckpointCertificate>>(nameof(CheckpointBridge)),
                sd.Get<CList<Checkpoint>>(nameof(ProofList))
            );
    }
}