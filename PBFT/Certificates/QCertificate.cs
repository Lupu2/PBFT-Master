namespace PBFT.Certificates
{
    public interface IQCertificate
    {
        public bool QReached(int nodes);

        public bool ProofsAreValid();

        public bool ValidateCertificate(int nodes);

        public void ResetCertificate();
    }
}