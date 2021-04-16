using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using PBFT.Messages;
using System.Linq;
using Newtonsoft.Json;
using PBFT.Helper;

namespace PBFT.Certificates
{
    public class ProtocolCertificate : IQCertificate, IPersistable
    { //Prepared, Commit phase Log.Add({1: seqnr, 2: viewnr, 3: prepared, 4: commit, 5: operation})
        public CertType CType {get; set;}
        public int SeqNr {get; set;}
        public int ViewNr {get; set;}
        public byte[] CurReqDigest {get; set;}
        private bool Valid{get; set;}
        public CList<PhaseMessage> ProofList {get; set;}
        
        public ProtocolCertificate(int seq, int vnr, byte[] req, CertType cType)
        {
                SeqNr = seq;
                ViewNr = vnr;
                CurReqDigest = req;
                CType = cType;
                Valid = false;
                ProofList = new CList<PhaseMessage>();
        }

        public ProtocolCertificate(int seq, int vnr, byte[] req, CertType cType, PhaseMessage firstrecord)
        {
            SeqNr = seq;
            ViewNr = vnr;
            CurReqDigest = req;
            CType = cType;
            Valid = false;
            ProofList = new CList<PhaseMessage>();
            ProofList.Add(firstrecord);
        }
        
        [JsonConstructor]
        public ProtocolCertificate(int seq, int vnr, byte[] req, CertType cType, bool val, CList<PhaseMessage> proof)
        {
            SeqNr = seq;
            ViewNr = vnr;
            CurReqDigest = req;
            CType = cType;
            Valid = val;
            ProofList = proof;
        }

        public bool QReached(int fNodes) => (ProofList.Count-AccountForDuplicates()) >= 2 * fNodes + 1;

        public bool QReached(int fNodes, CList<PhaseMessage> proofList) =>
            (proofList.Count - AccountForDuplicates(proofList) >= 2 * fNodes + 1);
        
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

        private int AccountForDuplicates(CList<PhaseMessage> proofList)
        {
            //Source: https://stackoverflow.com/questions/53512523/count-of-duplicate-items-in-a-c-sharp-list/53512576
            if (proofList.Count < 2) return 0;
            var count = proofList//there is an issue here
                .GroupBy(c => new {c.ServID, c.Signature})
                .Where(c => c.Count() > 1)
                .Sum(c => c.Count() - 1);
            return count;
        }

        //Checks that the Proofs are valid for PhaseMessages
        public bool ProofsAreValid()
        {
            int preparenr = 0;
            if (ProofList.Count < 1) return false;

            foreach (var proof in ProofList)
            {
                Console.WriteLine(proof);
                if (proof.Signature == null || proof.ViewNr != ViewNr || proof.SeqNr != SeqNr) return false;
                if (CurReqDigest == null && proof.Digest != null || CurReqDigest != null && proof.Digest == null) 
                    return false;
                if (CurReqDigest != null && proof.Digest != null && !CurReqDigest.SequenceEqual(proof.Digest))
                    return false;
                if (proof.PhaseType.Equals(PMessageType.PrePrepare) || proof.PhaseType.Equals(PMessageType.Prepare))
                {
                    if (proof.PhaseType.Equals(PMessageType.PrePrepare)) preparenr++;
                    if (CType != CertType.Prepared) return false;
                }
                else if (proof.PhaseType.Equals(PMessageType.Commit))
                {
                    if (CType != CertType.Committed) return false;
                }
                if (preparenr != 1 && CType.Equals(CertType.Prepared)) return false;
            }
            Console.WriteLine("ProofsAreValid are true");
            return true;
        }

        public bool ProofsAreValid(CList<PhaseMessage> proofs)
        {
            if (proofs.Count < 1) return false;
            int preparenr = 0;
            foreach (var proof in proofs)
            { 
                if (proof.Signature == null || proof.ViewNr != ViewNr || proof.SeqNr != SeqNr) return false;
                Console.WriteLine("Passed seqnr");
                if (CurReqDigest == null && proof.Digest != null || CurReqDigest != null && proof.Digest == null) 
                    return false;
                Console.WriteLine("passed digest");
                if (CurReqDigest != null && proof.Digest != null && !CurReqDigest.SequenceEqual(proof.Digest))
                    return false;
                Console.WriteLine("passed digest 2");
                if (proof.PhaseType == PMessageType.PrePrepare || proof.PhaseType == PMessageType.Prepare)
                {
                    if (proof.PhaseType.Equals(PMessageType.PrePrepare)) preparenr++;
                    if (CType != CertType.Prepared) return false;
                }
                else if (proof.PhaseType == PMessageType.Commit)
                {
                    if (CType != CertType.Committed) return false;
                }
                Console.WriteLine("Passed Message type");
                if (preparenr != 1 && CType.Equals(CertType.Prepared)) return false;
            }
            Console.WriteLine("ProofsAreValid are true");
            return true;
        }
        
        public bool ValidateCertificate(int fNodes)
        {
            if (!Valid)
            {
                Console.WriteLine("Current Proofs:");
                foreach (var proof in ProofList) Console.WriteLine(proof);    
            }
            if (!Valid)
            {
                Console.WriteLine("QReached: " + QReached(fNodes));
                if (QReached(fNodes) && ProofsAreValid()) Valid = true;
                else Console.WriteLine("Certificate is not valid!");
            }
            /*if (Valid) //TODO: debugging, remove before delivery
            {
                Console.WriteLine("Proofs:");
                foreach (var proof in ProofList) Console.WriteLine(proof);    
            }*/
            return Valid;
        }

        public bool ValidateProofs(int fNodes, CList<PhaseMessage> proofs)
        {
            if (!Valid)
            {
                Console.WriteLine("Current Proofs:");
                foreach (var proof in proofs) Console.WriteLine(proof);    
            }
            if (!Valid) 
                if (QReached(fNodes, proofs) && ProofsAreValid(proofs)) Valid = true;
                else Console.WriteLine("Certificate is not valid!");
            return Valid;
        }
        
        public bool IsValid => Valid;

        public void ResetCertificate() //Mostly used for debugging, might be useful if a certificate is deemed corrupted
        {
            Valid = false;
            ProofList = new CList<PhaseMessage>();
        }

        public ProtocolCertificate CloneInfoCertificate() =>
            new ProtocolCertificate(SeqNr, ViewNr, CurReqDigest, CType, Valid, new CList<PhaseMessage>());
        
        public override string ToString() => $"CertType:{CType}, SeqNr:{SeqNr}, ViewNr:{ViewNr}";

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            //throw new System.NotImplementedException();
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(CurReqDigest), Serializer.SerializeHash(CurReqDigest));
            stateToSerialize.Set(nameof(CType), (int)CType);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }
        
        private static ProtocolCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ProtocolCertificate(
                sd.Get<int>(nameof(SeqNr)),
                sd.Get<int>(nameof(ViewNr)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(CurReqDigest))),
                Enums.ToEnumCertType(sd.Get<int>(nameof(CType))),
                sd.Get<bool>(nameof(Valid)),
                sd.Get<CList<PhaseMessage>>(nameof(ProofList))
                );
    }
}