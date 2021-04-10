using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Helper.JsonObjects
{
    public class JsonViewChangeCertificate
    {
        public ViewPrimary ViewInfo { get; set; }
        public bool Valid { get; set; }
        public bool CalledShutdown { get; set; }
        public JsonCheckpointCertificate CurSystemState { get; set; }
        public List<ViewChange> ProofList { get; set; }

        [JsonConstructor]
        public JsonViewChangeCertificate(
            ViewPrimary viewinfo, 
            bool valid, 
            bool calledshut,
            JsonCheckpointCertificate curstatem, 
            List<ViewChange> prooflist)
        {
            ViewInfo = viewinfo;
            Valid = valid;
            CalledShutdown = calledshut;
            CurSystemState = curstatem;
            ProofList = prooflist;
        }

        public static JsonViewChangeCertificate ConvertToJsonViewChangeCertificate(ViewChangeCertificate vcc)
        {
            var list = new List<ViewChange>();
            foreach (var vc in vcc.ProofList) list.Add(vc);
            JsonCheckpointCertificate jsoncheckcert;
            if (vcc.CurSystemState != null)
                jsoncheckcert = JsonCheckpointCertificate.ConvertToJsonCheckpointCertificate(vcc.CurSystemState);
            else jsoncheckcert = null;
            var jsonvcc = new JsonViewChangeCertificate(vcc.ViewInfo, vcc.IsValid(), vcc.CalledShutdown,jsoncheckcert, list);
            return jsonvcc;
        }

        public ViewChangeCertificate ConvertToViewChangeCertificate()
        {
            var clist = new CList<ViewChange>();
            foreach (var vc in ProofList) clist.Add(vc);
            CheckpointCertificate checkcert;
            if (CurSystemState != null)
                checkcert = CurSystemState.ConvertToCheckpointCertificate();
            else checkcert = null;
            var vcc = new ViewChangeCertificate(ViewInfo, Valid, CalledShutdown, null, null, checkcert, clist);
            return vcc;
        }
    }
}