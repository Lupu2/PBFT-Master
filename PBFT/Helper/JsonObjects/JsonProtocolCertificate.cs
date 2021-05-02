using System.Collections.Generic;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Messages;

namespace PBFT.Helper.JsonObjects
{
    public class JsonProtocolCertificate
    {
        public CertType CType { get; set; }
        public int SeqNr { get; set; }
        public int ViewNr { get; set; }
        public byte[] CurReqDigest { get; set; }
        public bool Valid{ get; set; }
        public List<PhaseMessage> ProofList { get; set; }

        [JsonConstructor]
        public JsonProtocolCertificate(int seq, int vnr, byte[] req, CertType cType, bool val, List<PhaseMessage> proof)
        {
            SeqNr = seq;
            ViewNr = vnr;
            CType = cType;
            CurReqDigest = req;
            Valid = val;
            ProofList = proof;
        }

        public static JsonProtocolCertificate ConvertToJsonProtocolCertificate(ProtocolCertificate pcert)
        {
            var list = new List<PhaseMessage>();
            if (pcert.ProofList != null)
                foreach (var pmes in pcert.ProofList)
                    list.Add(pmes);
            else list = null;
            var jsonprotcert = new JsonProtocolCertificate(
                pcert.SeqNr, 
                pcert.ViewNr, 
                pcert.CurReqDigest, 
                pcert.CType, 
                pcert.IsValid, 
                list
            );
            return jsonprotcert;
        }

        public ProtocolCertificate ConvertToProtocolCertificate()
        {
            var clist = new CList<PhaseMessage>();
            foreach (var pmes in ProofList) clist.Add(pmes);
            var protcert = new ProtocolCertificate(SeqNr, ViewNr, CurReqDigest, CType, Valid, clist);
            return protcert;
        }
    }
}