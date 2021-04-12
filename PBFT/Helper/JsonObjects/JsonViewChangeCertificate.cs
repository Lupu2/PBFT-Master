using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;
using PBFT.Messages;
using PBFT.Replica;
using PBFT.Tests.Helper;

namespace PBFT.Helper.JsonObjects
{
    public class JsonViewChangeCertificate
    {
        public ViewPrimary ViewInfo { get; set; }
        public bool Valid { get; set; }
        public bool CalledShutdown { get; set; }
        public JsonCheckpointCertificate CurSystemState { get; set; }
        public List<JsonViewChange> ProofList { get; set; }

        [JsonConstructor]
        public JsonViewChangeCertificate(
            ViewPrimary viewinfo, 
            bool valid, 
            bool calledshut,
            JsonCheckpointCertificate curstatem, 
            List<JsonViewChange> prooflist)
        {
            ViewInfo = viewinfo;
            Valid = valid;
            CalledShutdown = calledshut;
            CurSystemState = curstatem;
            ProofList = prooflist;
        }

        public static JsonViewChangeCertificate ConvertToJsonViewChangeCertificate(ViewChangeCertificate vcc)
        {
            var list = new List<JsonViewChange>();
            if (vcc.ProofList != null)
                foreach (var vc in vcc.ProofList)
                    list.Add(JsonViewChange.ConvertToJsonViewChange(vc));
            else list = null;
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
            if (ProofList != null)
                foreach (var vc in ProofList)
                    clist.Add(vc.ConvertToViewChange());
            else clist = null;
            CheckpointCertificate checkcert;
            if (CurSystemState != null)
                checkcert = CurSystemState.ConvertToCheckpointCertificate();
            else checkcert = null;
            var vcc = new ViewChangeCertificate(ViewInfo, Valid, CalledShutdown, null, null, checkcert, clist);
            return vcc;
        }
    }
}