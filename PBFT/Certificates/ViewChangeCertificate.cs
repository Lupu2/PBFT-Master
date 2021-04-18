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
    public class ViewChangeCertificate : IQActionCertificate, IPersistable
    {
        public ViewPrimary ViewInfo { get; set; }
        private bool Valid { get; set; }
        public bool CalledShutdown { get; set; }
        public CheckpointCertificate CurSystemState { get; set; }
        public CList<ViewChange> ProofList { get; set; }
        public Action EmitShutdown;
        public Action EmitViewChange;
        
        public ViewChangeCertificate(ViewPrimary info, CheckpointCertificate state, Action shutdown, Action viewchange)
        {
            ViewInfo = info;
            Valid = false;
            CurSystemState = state;
            EmitShutdown = shutdown;
            EmitViewChange = viewchange;
            if (EmitShutdown != null) CalledShutdown = false;
            else CalledShutdown = true;
            ProofList = new CList<ViewChange>();
        }

        [JsonConstructor]
        public ViewChangeCertificate(ViewPrimary info, bool valid, bool shutdown, Action shutdownac, Action viewchange, CheckpointCertificate state, CList<ViewChange> proofs)
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
                Console.WriteLine(vc);
                if (vc.NextViewNr != ViewInfo.ViewNr) 
                    return false;
                if (CurSystemState == null && vc.CertProof != null || CurSystemState != null && vc.CertProof == null)
                    return false;
                if (CurSystemState != null && vc.CertProof != null && !CurSystemState.StateDigest.SequenceEqual(vc.CertProof.StateDigest)) 
                    return false;
                if (CurSystemState != null && vc.CertProof != null && CurSystemState.LastSeqNr != vc.StableSeqNr)
                    return false;
                if (vc.CertProof != null && !vc.CertProof.Stable) 
                    return false;
            }
            Console.WriteLine("PROOFS ARE VALID");
            return true;
        }

        public bool ValidateCertificate(int nodes)
        {
            Console.WriteLine("Validate Certificate");
            if (QReached(nodes) && ProofsAreValid()) Valid = true;
            return Valid;
        }
        
        public bool IsValid() => Valid;
        
        public void ResetCertificate(List<Action> actions)
        {
            CalledShutdown = false;
            Valid = false;
            ProofList = new CList<ViewChange>();
            EmitShutdown = actions[0];
            EmitViewChange = actions[1];
        }

        public void ScaleDownViewProofs()
        {
            foreach (var proof in ProofList) proof.RemoveUnecessaryData();
        }

        public void AppendViewChange(ViewChange vc, RSAParameters pubkey, int fnodes)
        {
            Console.WriteLine("AppendViewChange");
            if (vc.Validate(pubkey, ViewInfo.ViewNr))
            {
                Console.WriteLine("Adding");
                ProofList.Add(vc);
            }
            Console.WriteLine($"New Count: {ProofList.Count}");
            Verification(fnodes);
        }
        
        private void EmitShutdownHandler()
        {
            Console.WriteLine("Calling shutdown callback");
            if (EmitShutdown != null)
            {
                EmitShutdown();
                CalledShutdown = true;
                EmitShutdown = null;
            }
            
        }

        private void EmitViewChangeHandler()
        {
            Console.WriteLine("Calling view-change callback");
            if (EmitViewChange != null) EmitViewChange();
            EmitViewChange = null;
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
        
        private static ViewChangeCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewChangeCertificate(
                    sd.Get<ViewPrimary>(nameof(ViewInfo)),
                    sd.Get<bool>(nameof(Valid)),
                    sd.Get<bool>(nameof(CalledShutdown)),
                    sd.Get<Action>(nameof(EmitShutdown)),
                    sd.Get<Action>(nameof(EmitViewChange)),
                    sd.Get<CheckpointCertificate>(nameof(CurSystemState)),
                    sd.Get<CList<ViewChange>>(nameof(ProofList))
                );
    }
}