using System;
using System.Collections.Generic;

namespace PBFT.Certificates
{
    public interface IQActionCertificate
    {
        public bool QReached(int nodes);

        public bool ProofsAreValid();

        public bool ValidateCertificate(int nodes);

        public void ResetCertificate(List<Action> actions);
    }
}