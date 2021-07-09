namespace PBFT.Certificates
{
    //IQCertificate is an interface for our protocol certificate implementations.
    //The IQCertificate contains the necessary functions for an object implementation to act as a PBFT certificate.
    public interface IQCertificate
    {
        public bool QReached(int nodes); //checks whether or not the desired quorum condition is met or not.

        public bool ProofsAreValid(); //checks that all the proofs stored in the proof lists are valid.

        public bool ValidateCertificate(int nodes); //validates whether or not the certificate has become valid/stable.

        public void ResetCertificate(); //resets the proof list in addition to the certificate status.

        public void SeeProofs(); //see all the proofs currently in the proof list.
    }
}