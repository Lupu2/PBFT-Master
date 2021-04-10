using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;
using PBFT.Messages;

namespace PBFT.Helper.JsonObjects
{
    public class JsonNewView
    {
        public int NewViewNr { get; set; }
        public JsonViewChangeCertificate ViewProof { get; set; }
        public List<PhaseMessage> PrePrepMessages { get; set; }
        public byte[] Signature { get; set; }

        [JsonConstructor]
        public JsonNewView(int newview, JsonViewChangeCertificate viewproof, List<PhaseMessage> prepremes, byte[] sign)
        {
            NewViewNr = newview;
            ViewProof = viewproof;
            PrePrepMessages = prepremes;
            Signature = sign;
        }

        public static JsonNewView ConvertToJsonNewViewCertificate(NewView nv)
        {
            var list = new List<PhaseMessage>();
            foreach (var premes in nv.PrePrepMessages) list.Add(premes);
            JsonViewChangeCertificate jsonvcc;
            if (nv.ViewProof != null)
                jsonvcc = JsonViewChangeCertificate.ConvertToJsonViewChangeCertificate(nv.ViewProof);
            else jsonvcc = null;
            var jsonnv = new JsonNewView(nv.NewViewNr, jsonvcc, list, nv.Signature);
            return jsonnv;
        }

        public NewView ConvertToNewView()
        {
            var clist = new CList<PhaseMessage>();
            foreach (var premes in PrePrepMessages) clist.Add(premes);
            ViewChangeCertificate vcc;
            if (ViewProof != null)
                vcc = ViewProof.ConvertToViewChangeCertificate();
            else vcc = null;
            var nv = new NewView(NewViewNr, vcc, clist, Signature);
            return nv;
        }
    }
}