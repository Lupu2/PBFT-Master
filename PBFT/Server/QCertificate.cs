using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Messages;
using System.Linq;

namespace PBFT.Server
{
 
 public enum CertType {
     Prepared,
     Committed,

     Reply,
     Checkpoint,
     ViewChange,

 }
    public class QCertificate : IPersistable
    {
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
        }


        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            //throw new System.NotImplementedException();
            stateToSerialize.Set(nameof(Type), Type);
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(Valid), Valid);
            stateToSerialize.Set(nameof(ProofList), ProofList);
        }

        private bool QReached(int FNodes)
        {
            return ProofList.Count >= 2 * FNodes + 1;
        }

        private bool CheckForDuplicates()
        {
            return ProofList.GroupBy(c => new {c.ServID, c.Signature})
                            .Any(p => p.Count() > 1); //https://stackoverflow.com/questions/16197290/checking-for-duplicates-in-a-list-of-objects-c-sharp/16197491
            
        }

        public bool ValidateCertificate(int FNodes) //potentially asynchrous together with QReached
        {
            if (!Valid) if (QReached(FNodes) && !CheckForDuplicates()) Valid = true;
            return Valid;
        }
    }
}