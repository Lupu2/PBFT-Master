using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace PBFT.Certificates
{
    public class ViewChangeCertificate : IQCertificate, IPersistable
    {
        public bool QReached(int nodes)
        {
            throw new System.NotImplementedException();
        }

        public bool ProofsAreValid()
        {
            throw new System.NotImplementedException();
        }

        public bool ValidateCertificate(int nodes)
        {
            throw new System.NotImplementedException();
        }

        public void ResetCertificate()
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            throw new System.NotImplementedException();
        }
        
        
    }
}