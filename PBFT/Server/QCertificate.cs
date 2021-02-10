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

namespace PBFT.Server
{
 
 
    public class QCertificate : IPersistable
    { //Prepared, Commit phase Log.Add({1: seqnr, 2: viewnr, 3: prepared, 4: commit, 5: operation})
        public CertType Type {get; set;}
        public int SeqNr;
        public int ViewNr;

        private bool Valid{get; set;}

        public CList<PhaseMessage> ProofList {get; set;}
        public QCertificate(CertType type, int seq, int vnr)
        {
                Type = type;
                SeqNr = seq;
                ViewNr = vnr;
                Valid = false;
                ProofList = new CList<PhaseMessage>();
                
        }
        
        [JsonConstructor]
        public QCertificate(CertType type, int seq, int vnr, bool val, CList<PhaseMessage> proof)
        {
            Type = type;
            SeqNr = seq;
            ViewNr = vnr;
            Valid = false;
            ProofList = proof;
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            //throw new System.NotImplementedException();
            stateToSerialize.Set(nameof(Type), (int)Type);
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }
        
        private static QCertificate Deserialize(IReadOnlyDictionary<string, object> sd)
            => new QCertificate(
                Enums.ToEnumCertType(sd.Get<int>(nameof(Type))),
                sd.Get<int>(nameof(SeqNr)),
                sd.Get<int>(nameof(ViewNr)),
                sd.Get<bool>(nameof(Valid)),
                sd.Get<CList<PhaseMessage>>(nameof(ProofList))
                );
        

        private bool QReached(int fNodes) => ProofList.Count >= 2 * fNodes + 1;
        
        private bool CheckForDuplicates()
        {
            return ProofList.GroupBy(c => new {c.ServID, c.Signature})
                            .Any(p => p.Count() > 1); //https://stackoverflow.com/questions/16197290/checking-for-duplicates-in-a-list-of-objects-c-sharp/16197491
            
        }

        public bool ValidateCertificate(int fNodes) //potentially asynchrous together with QReached
        {
            if (!Valid) if (QReached(fNodes) && !CheckForDuplicates()) Valid = true;
            return Valid;
        }
    }
}