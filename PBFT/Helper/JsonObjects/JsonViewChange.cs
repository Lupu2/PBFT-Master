using System.Collections.Generic;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Helper.JsonObjects;
using PBFT.Messages;

namespace PBFT.Tests.Helper
{
    public class JsonViewChange
    {
        public int StableSeqNr { get; set; }
        public int ServID { get; set; }
        public int NextViewNr { get; set; }
        public JsonCheckpointCertificate CertProof { get; set; }
        public Dictionary<int, JsonProtocolCertificate> RemPreProofs { get; set; }
        public byte[] Signature { get; set; }
        
        [JsonConstructor]
        public JsonViewChange(int stableSeq, int rid, int newViewNr, JsonCheckpointCertificate cproof, Dictionary<int, JsonProtocolCertificate> prepcerts, byte[] sign)
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProof = cproof;
            RemPreProofs = prepcerts;
            Signature = sign;
        }

        public static JsonViewChange ConvertToJsonViewChange(ViewChange vc)
        {
            var dict = new Dictionary<int, JsonProtocolCertificate>();
            if (vc.RemPreProofs != null)
                foreach (var (key, protocert) in vc.RemPreProofs)
                    dict[key] = JsonProtocolCertificate.ConvertToJsonProtocolCertificate(protocert);
            else dict = null;
            JsonCheckpointCertificate jsoncheckcert;
            if (vc.CertProof != null)
                jsoncheckcert = JsonCheckpointCertificate.ConvertToJsonCheckpointCertificate(vc.CertProof);
            else jsoncheckcert = null;
            var jsonvc = new JsonViewChange(vc.StableSeqNr, vc.ServID, vc.NextViewNr, jsoncheckcert, dict, vc.Signature);
            return jsonvc;
        }

        public ViewChange ConvertToViewChange()
        {
            var cdict = new CDictionary<int, ProtocolCertificate>();
            if (RemPreProofs != null)
                foreach (var (key, jsonprotocert) in RemPreProofs)
                    cdict[key] = jsonprotocert.ConvertToProtocolCertificate();
            else cdict = null;
            CheckpointCertificate checkcert;
            if (CertProof != null) checkcert = CertProof.ConvertToCheckpointCertificate();
            else checkcert = null;
            var vc = new ViewChange(StableSeqNr, ServID, NextViewNr, checkcert, cdict, Signature);
            return vc;
        }
    }
}