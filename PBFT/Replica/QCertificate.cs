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
using PBFT.Helper;

namespace PBFT.Replica
{
 
 
    public class QCertificate : IPersistable
    { //Prepared, Commit phase Log.Add({1: seqnr, 2: viewnr, 3: prepared, 4: commit, 5: operation})
        public CertType Type {get; set;}
        public int SeqNr;
        public int ViewNr;
        private bool Valid{get; set;}

        public CList<PhaseMessage> ProofList {get; set;}
        public QCertificate(int seq, int vnr, CertType type)
        {
                SeqNr = seq;
                ViewNr = vnr;
                Type = type;
                Valid = false;
                ProofList = new CList<PhaseMessage>();
        }

        public QCertificate(int seq, int vnr, CertType type, PhaseMessage firstrecord)
        {
            SeqNr = seq;
            ViewNr = vnr;
            Type = type;
            Valid = false;
            ProofList = new CList<PhaseMessage>();
            ProofList.Add(firstrecord);
        }
        
        [JsonConstructor]
        public QCertificate(int seq, int vnr, CertType type, bool val, CList<PhaseMessage> proof)
        {
            SeqNr = seq;
            ViewNr = vnr;
            Type = type;
            Valid = false;
            ProofList = proof;
        }

        private bool QReached(int fNodes) => (ProofList.Count-AccountForDuplicates()) >= 2 * fNodes + 1;
        
        
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
        private bool ProofsArePMsValid() 
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
                
                if (proof.MessageType == PMessageType.PrePrepare || proof.MessageType == PMessageType.Prepare)
                {
                    if (Type != CertType.Prepared)
                    {
                        proofvalid = false;
                        break;
                    }
                }
                else if (proof.MessageType == PMessageType.Commit)
                {
                    if (Type != CertType.Committed)
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
            if (!Valid) 
                if (QReached(fNodes) && ProofsArePMsValid()) Valid = true;
            return Valid;
        }

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
            stateToSerialize.Set(nameof(Type), (int)Type);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }
        
        private static QCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new QCertificate(
                sd.Get<int>(nameof(SeqNr)),
                sd.Get<int>(nameof(ViewNr)),
                Enums.ToEnumCertType(sd.Get<int>(nameof(Type))),
                sd.Get<bool>(nameof(Valid)),
                sd.Get<CList<PhaseMessage>>(nameof(ProofList))
                );
    }
}