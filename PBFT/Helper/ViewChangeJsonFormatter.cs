using System.Collections.Generic;
using Newtonsoft.Json;
using PBFT.Certificates;

namespace PBFT.Tests.Helper
{
    public class ViewChangeJsonFormatter
    {
        public int StableSeqNr { get; set; }
        public int ServID { get; set; }
        public int NextViewNr { get; set; }
        public string CertProof { get; set; }

        public string RemPreProofs { get; set; }

        public byte[] Signature { get; set; }


        [JsonConstructor]
        public ViewChangeJsonFormatter(int stableSeq, int rid, int newViewNr, string cproof, string prepcerts, byte[] sign)
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProof = cproof;
            RemPreProofs = prepcerts;
            Signature = sign;
        }
    }
}