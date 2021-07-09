using System;
using System.Collections.Generic;

namespace PBFT.Certificates
{
    //IQActionCertificate is an interface for our protocol certificates implementations
    //that were required to perform an action immediately after achieving the desired number of proofs.
    public interface IQActionCertificate
    {
        public bool QReached(int nodes);

        public bool ProofsAreValid();

        public bool ValidateCertificate(int nodes);

        public void ResetCertificate(List<Action> actions);
    }
}