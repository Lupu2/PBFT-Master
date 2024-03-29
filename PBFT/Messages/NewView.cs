using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Text;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Helper.JsonObjects;

namespace PBFT.Messages
{
    public class NewView : IProtocolMessages, ISignedMessage, IPersistable
    {
        public int NewViewNr { get; set; }
        public ViewChangeCertificate ViewProof { get; set; }
        public CList<PhaseMessage> PrePrepMessages { get; set; }
        public byte[] Signature { get; set; }

        public NewView(int viewnr, ViewChangeCertificate vProof, CList<PhaseMessage> preprepmes)
        {
            NewViewNr = viewnr;
            ViewProof = vProof;
            PrePrepMessages = preprepmes;
        }
        
        [JsonConstructor]
        public NewView(int viewnr, ViewChangeCertificate vProof, CList<PhaseMessage> preprepmes, byte[] sign)
        {
            NewViewNr = viewnr;
            ViewProof = vProof;
            PrePrepMessages = preprepmes;
            Signature = sign;
        }
        
        public byte[] SerializeToBuffer()
        {
            Action tempshut = null;
            Action tempview = null;
            if (ViewProof != null)
            {
                tempshut = ViewProof.EmitShutdown;
                tempview = ViewProof.EmitViewChange;
                ViewProof.EmitShutdown = null;
                ViewProof.EmitViewChange = null;
            }
            var jsonnv = JsonNewView.ConvertToJsonNewView(this);
            string jsonval = JsonConvert.SerializeObject(jsonnv);
            if (tempshut != null) ViewProof.EmitShutdown = tempshut;
            if (tempview != null) ViewProof.EmitViewChange = tempview;
                return Encoding.ASCII.GetBytes(jsonval);
        }

        public byte[] SerializeToBufferSignature()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }
        public static NewView DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
            var jsonnv = JsonConvert.DeserializeObject<JsonNewView>(jsonobj);
            return jsonnv.ConvertToNewView();
        }
        
        public void SignMessage(RSAParameters prikey, string haspro = "SHA256")
        {
            using (var rsa = RSA.Create())
            {
                byte[] hashmes;
                using (var shaalgo = SHA256.Create())
                {
                    var serareq = SerializeToBufferSignature();
                    hashmes = shaalgo.ComputeHash(serareq);
                }
                rsa.ImportParameters(prikey);
                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                rsaFormatter.SetHashAlgorithm(haspro);
                rsaFormatter.SetKey(rsa);
                Signature = rsaFormatter.CreateSignature(hashmes);
            }
        }

        public bool Validate(RSAParameters pubkey, int nextview)
        {
            Console.WriteLine("Validating NewView");
            var copymes = (NewView) CreateCopyTemplate();
            if (!Crypto.VerifySignature(Signature, copymes.SerializeToBufferSignature(), pubkey)) return false;
            if (NewViewNr != nextview) return false;
            foreach (var prepre in PrePrepMessages)
            {
                var copypre = prepre.CreateCopyTemplate();
                if (!Crypto.VerifySignature(prepre.Signature, copypre.SerializeToBuffer(), pubkey)) return false;
                if (prepre.PhaseType != PMessageType.PrePrepare) return false;
            } ;
            if (ViewProof == null) return false;
            if (!ViewProof.IsValid()) return false;
            Console.WriteLine("All tests passes");
            return true;
        }
        
        public IProtocolMessages CreateCopyTemplate() => new NewView(NewViewNr, ViewProof, PrePrepMessages);
        
        public override string ToString()
        {
            bool sign;
            if (Signature != null) sign = true;
            else sign = false;
            string text = $"NewViewNr: {NewViewNr}, is Signed: {sign}\n";
            if (ViewProof != null)
            {
                text += $"ViewChange Certificate: ViewPrimary: {ViewProof.ViewInfo}, Proofs: \n";
                foreach (var proof in ViewProof.ProofList)
                    text += $"{proof}, ";
            }
            text += "Prepare messages:\n";
            if (PrePrepMessages != null)
                foreach (var prep in PrePrepMessages)
                    text += $"{prep}, ";
            return text;
        }

        public bool Compare(NewView nvc2)
        {
            if (nvc2.NewViewNr != NewViewNr) return false;
            if (nvc2.PrePrepMessages.Count != PrePrepMessages.Count) return false;
            for (int i=0; i<PrePrepMessages.Count; i++)
            {
                if (!nvc2.PrePrepMessages[i].Compare(PrePrepMessages[i])) return false;
            }
            if (nvc2.ViewProof == null && ViewProof != null || nvc2.ViewProof != null && ViewProof == null) return false;
            if (nvc2.ViewProof != null && ViewProof != null)
            {
                if (nvc2.ViewProof.IsValid() != ViewProof.IsValid()) return false;
                if (nvc2.ViewProof.CalledShutdown != ViewProof.CalledShutdown) return false;
                for(int j=0; j<ViewProof.ProofList.Count; j++)
                {
                    if (!nvc2.ViewProof.ProofList[j].Compare(ViewProof.ProofList[j])) return false;
                }
            }
            if (nvc2.Signature == null && Signature != null || nvc2.Signature != null && Signature == null) return false;
            if (nvc2.Signature != null && Signature != null && !nvc2.Signature.SequenceEqual(Signature)) return false;
            return true;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(NewViewNr),NewViewNr);
            stateToSerialize.Set(nameof(ViewProof), ViewProof);
            stateToSerialize.Set(nameof(PrePrepMessages), PrePrepMessages);
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }

        private static NewView Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new NewView(
                sd.Get<int>(nameof(NewViewNr)),
                sd.Get<ViewChangeCertificate>(nameof(ViewProof)),
                sd.Get<CList<PhaseMessage>>(nameof(PrePrepMessages)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
            );
        }
    }
}