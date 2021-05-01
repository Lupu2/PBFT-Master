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
        public int LastSeqNr { get; set;}
        public byte[] StateDigest { get; set;}
        public bool Stable { get; set; }
        public CList<Checkpoint> ProofList { get; set; }
        public Action<CheckpointCertificate> EmitCheckpoint { get; set; }

        public CheckpointCertificate(int seqLimit, byte[] digest, Action<CheckpointCertificate> emitCheckpoint)
        {
            LastSeqNr = seqLimit;
            StateDigest = digest;
            Stable = false;
            ProofList = new CList<Checkpoint>();
            EmitCheckpoint = emitCheckpoint;
        }
        
        [JsonConstructor]
        public CheckpointCertificate(int seqLimit, byte[] digest, bool stable, Action<CheckpointCertificate> emitCheckpoint, CList<Checkpoint> proofs)
        {
            LastSeqNr = seqLimit;
            StateDigest = digest;
            Stable = stable;
            ProofList = proofs;
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
            Console.WriteLine("ProofsAreValid");
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

            Console.WriteLine("All proofs are valid");
            return true;
        }

        public bool ValidateCertificate(int nodes)
        {
            Console.WriteLine("ValidateCertificate");
            Console.WriteLine(ProofList.Count);
            if (QReached(nodes) && ProofsAreValid()) Stable = true;
            Console.WriteLine(Stable);
            return Stable;
        }

        public void ResetCertificate()
        {
            Stable = false;
            ProofList = new CList<Checkpoint>();
        }

        public void AppendProof(Checkpoint check, RSAParameters pubkey, int failureNr)
        {
            if (!Stable)
            {
                Console.WriteLine("Before:");
                //SeeProofs();
                if (check.Validate(pubkey) && check.StableSeqNr == LastSeqNr && check.StateDigest.SequenceEqual(StateDigest))
                {
                    Console.WriteLine("ADDING CHECKPOINT");
                    ProofList.Add(check);
                    Stable = ValidateCertificate(failureNr);
                    Console.WriteLine("Validation: " + Stable);
                    if (!Stable)
                    {
                        Console.WriteLine("After:");
                        SeeProofs();
                    }
                    if (Stable && EmitCheckpoint != null) EmitCertificate();    
                }
            }
        }

        public void SeeProofs()
        {
            foreach (var proof in ProofList)
                Console.WriteLine(proof);
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
            EmitCheckpoint(this);
            EmitCheckpoint = null;
        }

        public override string ToString()
        {
            
            var toString = $"StableSeq: {LastSeqNr}, Stable: {Stable}, Proofs: {ProofList.Count - AccountForDuplicates()}\n";
            foreach (var proof in ProofList) toString += proof + ", ";
            return toString;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(LastSeqNr), LastSeqNr);
            stateToSerialize.Set(nameof(StateDigest), Serializer.SerializeHash(StateDigest));
            stateToSerialize.Set(nameof(Stable), Stable);
            stateToSerialize.Set(nameof(EmitCheckpoint), EmitCheckpoint);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }
        
        private static CheckpointCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CheckpointCertificate(
                sd.Get<int>(nameof(LastSeqNr)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(StateDigest))),
                sd.Get<bool>(nameof(Stable)),
                sd.Get<Action<CheckpointCertificate>>(nameof(EmitCheckpoint)),
                sd.Get<CList<Checkpoint>>(nameof(ProofList))
            );
    }
}