using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using PBFT.Messages;
using System.Linq;
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
        
        private bool QReached(int fNodes) => ProofList.Count >= 2 * fNodes + 1;
        
        private bool CheckForDuplicates()
        {
            return ProofList.GroupBy(c => new {c.ServID, c.Signature})
                .Any(p => p.Count() > 1); //https://stackoverflow.com/questions/16197290/checking-for-duplicates-in-a-list-of-objects-c-sharp/16197491
            
        }

        public bool ValidateCertificate(int fNodes) //potentially asynchronous together with QReached
        {
            if (!Valid) if (QReached(fNodes) && !CheckForDuplicates()) Valid = true;
            return Valid;
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