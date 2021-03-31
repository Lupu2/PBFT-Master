using System;
using System.Linq;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Messages;

namespace PBFT.Certificates
{
    public class ReplyCertificate :IQCertificate
    {
        public Request RequestOrg {get; set;}
        
        public bool ValStrength { get; set; }
        
        private bool Valid{get; set;}

        public CList<Reply> ProofList {get; set;}

        public ReplyCertificate(Request req)
        {
            RequestOrg = req;
            ValStrength = false;
            Valid = false;
            ProofList = new CList<Reply>();
        }

        public ReplyCertificate(Request req, bool str)
        {
            RequestOrg = req;
            ValStrength = str;
            Valid = false;
            ProofList = new CList<Reply>();
        }

        public bool IsValid() => Valid;
        
        public bool WeakQReached(int fNodes) => (ProofList.Count-AccountForDuplicates()) >= fNodes + 1;

        public bool QReached(int fNodes) => (ProofList.Count - AccountForDuplicates()) >= 2 * fNodes + 1;

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
        
        public bool ProofsAreValid()
        {
            if (ProofList.Count < 1) return false;
            bool proofvalid = true;
            string curres = ProofList[0].Result;
            bool curstatus = ProofList[0].Status;
            foreach (var proof in ProofList)
            {
                if (proof.Timestamp != RequestOrg.Timestamp)
                {
                    proofvalid = false;
                    break;
                }
                Console.WriteLine("PASSED timestamp");
                if (proof.Signature == null || proof.Result.Equals("") || proof.Result == null || !curres.Equals(proof.Result) || curstatus != proof.Status)
                {
                    proofvalid = false;
                    break;
                }
                Console.WriteLine("PASSED result signature, status test");
            }
            return proofvalid;
        }
        
        public bool ValidateCertificate(int fNodes)
        {
            Console.WriteLine("Validating!");
            if (!Valid)
                if (ValStrength)
                {
                    if (WeakQReached(fNodes) && ProofsAreValid()) Valid = true;
                }
                else
                {
                    if (QReached(fNodes) && ProofsAreValid()) Valid = true;
                }
            Console.WriteLine(Valid);
            return Valid;
        }

        public void ResetCertificate()
        {
            Valid = false;
            ProofList = new CList<Reply>();
        }
    }
}