using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.ProtocolMessages;

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
            throw new System.NotImplementedException();
        }

        private bool QReached(int FNodes)
        {
            return ProofList.Count >= 2 * FNodes + 1;
        }

        public bool ValidateCertificate(int FNodes) //potentially asynchrous together with QReached
        {
            Valid = QReached(FNodes);
            return Valid;
        }
    }
}