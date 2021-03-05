using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using PBFT.Messages;
using System.Linq;
using System.Xml.XPath;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Helper;

namespace PBFT.Certificates
{
 
 
    public class ProtocolCertificate : IQCertificate, IPersistable
    { //Prepared, Commit phase Log.Add({1: seqnr, 2: viewnr, 3: prepared, 4: commit, 5: operation})
        public CertType CType {get; set;}
        public int SeqNr;
        public int ViewNr;
        private bool Valid{get; set; }

        public CList<PhaseMessage> ProofList {get; set;}
        public ProtocolCertificate(int seq, int vnr, CertType cType)
        {
                SeqNr = seq;
                ViewNr = vnr;
                CType = cType;
                Valid = false;
                ProofList = new CList<PhaseMessage>();
        }

        public ProtocolCertificate(int seq, int vnr, CertType cType, PhaseMessage firstrecord)
        {
            SeqNr = seq;
            ViewNr = vnr;
            CType = cType;
            Valid = false;
            ProofList = new CList<PhaseMessage>();
            ProofList.Add(firstrecord);
        }
        
        [JsonConstructor]
        public ProtocolCertificate(int seq, int vnr, CertType cType, bool val, CList<PhaseMessage> proof)
        {
            SeqNr = seq;
            ViewNr = vnr;
            CType = cType;
            Valid = false;
            ProofList = proof;
        }

        public bool QReached(int fNodes) => (ProofList.Count-AccountForDuplicates()) >= 2 * fNodes + 1;
        
        
        private int AccountForDuplicates()
        {
            //Source: https://stackoverflow.com/questions/53512523/count-of-duplicate-items-in-a-c-sharp-list/53512576
            if (ProofList.Count < 2) return 0;
            var count = ProofList
                .GroupBy(c => new {c.ServID, c.Signature})
                .Where(c => c.Count() > 1)
                .Sum(c => c.Count()-1);
            return count;
        }

        //Checks that the Proofs are valid for PhaseMessages
        public bool ProofsAreValid() 
        {
            if (ProofList.Count < 1) return false;
            bool proofvalid = true;
         
            foreach (var proof in ProofList)
            {
                //Console.WriteLine(proof);
                if (proof.Digest == null || proof.Signature == null || proof.ViewNr != ViewNr || proof.SeqNr != SeqNr)
                {
                    proofvalid = false;
                    break;
                }
                
                if (proof.PhaseType == PMessageType.PrePrepare || proof.PhaseType == PMessageType.Prepare)
                {
                    if (CType != CertType.Prepared)
                    {
                        proofvalid = false;
                        break;
                    }
                }
                else if (proof.PhaseType == PMessageType.Commit)
                {
                    if (CType != CertType.Committed)
                    {
                        proofvalid = false;
                        break;
                    } 
                }
            }
            return proofvalid;
        }
        
        public bool ValidateCertificate(int fNodes)
        {
            //foreach (var proof in ProofList) Console.WriteLine(proof);
            if (!Valid) 
                if (QReached(fNodes) && ProofsAreValid()) Valid = true;
                else Console.WriteLine("Certificate is not valid!");
            return Valid;
        }

        public bool IsValid => Valid;

        public void ResetCertificate() //Mostly used for debugging, might be useful if a certificate is deemed corrupted
        {
            Valid = false;
            ProofList = new CList<PhaseMessage>();
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            //throw new System.NotImplementedException();
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(CType), (int)CType);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }
        
        private static ProtocolCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ProtocolCertificate(
                sd.Get<int>(nameof(SeqNr)),
                sd.Get<int>(nameof(ViewNr)),
                Enums.ToEnumCertType(sd.Get<int>(nameof(CType))),
                sd.Get<bool>(nameof(Valid)),
                sd.Get<CList<PhaseMessage>>(nameof(ProofList))
                );
    }
}