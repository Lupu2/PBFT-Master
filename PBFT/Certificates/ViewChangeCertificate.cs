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
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Certificates
{
    public class ViewChangeCertificate : IQCertificate, IPersistable
    {
        public ViewPrimary ViewInfo { get; set; }
        public bool Valid { get; set; }
        public bool CalledShutdown { get; set; }
        public CheckpointCertificate CurSystemState { get; set; }
        public CList<ViewChange> ProofList { get; set; }
        public Action<ViewChangeCertificate> EmitShutdown;
        public Action EmitViewChange;
        
        public ViewChangeCertificate(ViewPrimary info, CheckpointCertificate state, Action<ViewChangeCertificate> shutdown, Action viewchange)
        {
            ViewInfo = info;
            Valid = false;
            CalledShutdown = false;
            CurSystemState = state;
            EmitShutdown = shutdown;
            EmitViewChange = viewchange;
            ProofList = new CList<ViewChange>();
        }

        [JsonConstructor]
        public ViewChangeCertificate(ViewPrimary info, bool valid, bool shutdown, Action<ViewChangeCertificate> shutdownac, Action viewchange ,CheckpointCertificate state, CList<ViewChange> proofs)
        {
            ViewInfo = info;
            Valid = valid;
            CalledShutdown = shutdown;
            CurSystemState = state;
            EmitShutdown = shutdownac;
            EmitViewChange = viewchange;
            ProofList = proofs;
        }
        
        public bool QReached(int nodes) => CalculateNrOfValidProofs() >= 2 * nodes + 1;

        public bool ShutdownReached(int nodes) => CalculateNrOfValidProofs() >= 2 * nodes;
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

        private int CalculateNrOfValidProofs() => ProofList.Count - AccountForDuplicates();

        private void Verification(int nodes)
        {
            if (!Valid)
            {
                if (ShutdownReached(nodes) && ProofsAreValid() && !CalledShutdown) EmitShutdownHandler();
                bool res = ValidateCertificate(nodes);
                if (Valid && res) EmitViewChangeHandler();    //res and Valid should always be true together
            }
        }
        
        public bool ProofsAreValid()
        {
            Console.WriteLine("Checking proofs");
            foreach (var vc in ProofList)
            {
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
            Console.WriteLine("PROOFS ARE VALID");
            return true;
        }

        public bool ValidateCertificate(int nodes)
        {
            if (QReached(nodes) && ProofsAreValid()) Valid = true;
            return Valid;
        }
        
        public void ResetCertificate()
        {
            CalledShutdown = false;
            Valid = false;
            ProofList = new CList<ViewChange>();
        }

        public void AppendViewChange(ViewChange vc, RSAParameters pubkey, int fnodes)
        {
            if (vc.Validate(pubkey, ViewInfo.ViewNr)) 
                ProofList.Add(vc);
            Verification(fnodes);
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ViewInfo), ViewInfo);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(CalledShutdown), CalledShutdown);
            stateToSerialize.Set(nameof(EmitShutdown), EmitShutdown);
            stateToSerialize.Set(nameof(EmitViewChange), EmitViewChange);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }

        private void EmitShutdownHandler()
        {
            Console.WriteLine("Calling shutdown callback");
            EmitShutdown(this);
            CalledShutdown = true;
        }

        private void EmitViewChangeHandler()
        {
            Console.WriteLine("Calling viewchange callback");
            EmitViewChange();
        }
        
        private static ViewChangeCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewChangeCertificate(
                    sd.Get<ViewPrimary>(nameof(ViewInfo)),
                    sd.Get<bool>(nameof(Valid)),
                    sd.Get<bool>(nameof(CalledShutdown)),
                    sd.Get<Action<ViewChangeCertificate>>(nameof(EmitShutdown)),
                    sd.Get<Action>(nameof(EmitViewChange)),
                    sd.Get<CheckpointCertificate>(nameof(CurSystemState)),
                    sd.Get<CList<ViewChange>>(nameof(ProofList))
                );


    }
}