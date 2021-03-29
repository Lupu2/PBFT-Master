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
using PBFT.Replica;

namespace PBFT.Certificates
{
    public class ViewChangeCertificate : IQCertificate, IPersistable
    {
        public ViewPrimary ViewInfo { get; set; }
        public bool Valid { get; set; }
        
        public CheckpointCertificate CurSystemState { get; set; }
        public CList<ViewChange> ProofList { get; set; }

        public ViewChangeCertificate(ViewPrimary info, CheckpointCertificate state)
        {
            ViewInfo = info;
            Valid = false;
            CurSystemState = state;
            ProofList = new CList<ViewChange>();
        }

        [JsonConstructor]
        public ViewChangeCertificate(ViewPrimary info, bool valid, CheckpointCertificate state, CList<ViewChange> proofs)
        {
            ViewInfo = info;
            Valid = valid;
            CurSystemState = state;
            ProofList = proofs;
        }
        
        public bool QReached(int nodes) => (ProofList.Count - AccountForDuplicates()) >= 2 * nodes + 1;
        
        private int AccountForDuplicates()
        {
            //Source: https://stackoverflow.com/questions/53512523/count-of-duplicate-items-in-a-c-sharp-list/53512576
            if (ProofList.Count < 2) return 0;
            var count = ProofList//there is an issue here
                .GroupBy(c => new {c.ServID, c.Signature})
                .Where(c => c.Count() > 1)
                .Sum(c => c.Count()-1);
            Console.WriteLine(count);
            return count;
        }
        
        public bool ProofsAreValid()
        {
            Console.WriteLine("PROOFS ARE VALID");
            foreach (var vc in ProofList)
            {
                /*
                public int StableSeqNr { get; set; }
                public int ServID { get; set; }
                public CheckpointCertificate CertProofs { get; set; }
                public CDictionary<int, ProtocolCertificate> RemPreProofs { get; set;}
                */
                
                if (vc.NextViewNr != ViewInfo.ViewNr) 
                    return false;
                if (CurSystemState == null && vc.CertProofs != null || CurSystemState != null && vc.CertProofs == null)
                    return false;
                if (CurSystemState != null && vc.CertProofs != null && !CurSystemState.StateDigest.SequenceEqual(vc.CertProofs.StateDigest)) 
                    return false;
                if (CurSystemState != null && vc.CertProofs != null && CurSystemState.LastSeqNr != vc.StableSeqNr)
                    return false;
                if (vc.CertProofs != null && !vc.CertProofs.Stable) 
                    return false;
            }
            return true;
        }

        public bool ValidateCertificate(int nodes)
        {
            if (QReached(nodes) && ProofsAreValid()) Valid = true;
            return Valid;
        }
        
        public void ResetCertificate()
        {
            Valid = false;
            ProofList = new CList<ViewChange>();
        }

        public void AppendViewChange(ViewChange vc, RSAParameters pubkey)
        {
            if (vc.Validate(pubkey, ViewInfo.ViewNr)) ProofList.Add(vc);
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ViewInfo), ViewInfo);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }

        private static ViewChangeCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewChangeCertificate(
                    sd.Get<ViewPrimary>(nameof(ViewInfo)),
                    sd.Get<bool>(nameof(Valid)),
                    sd.Get<CheckpointCertificate>(nameof(CurSystemState)),
                    sd.Get<CList<ViewChange>>(nameof(ProofList))
                );


    }
}