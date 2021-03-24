using System.Collections.Generic;
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

namespace PBFT.Messages
{
    public class NewView : IProtocolMessages, SignedMessage, IPersistable
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
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }
        public static NewView DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<NewView>(jsonobj);
        }
        public void SignMessage(RSAParameters prikey, string haspro = "SHA256")
        {
            using (var rsa = RSA.Create())
            {
                byte[] hashmes;
                using (var shaalgo = SHA256.Create())
                {
                    var serareq = this.SerializeToBuffer();
                    hashmes = shaalgo.ComputeHash(serareq);
                }
                rsa.ImportParameters(prikey);
                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                rsaFormatter.SetHashAlgorithm(haspro);
                rsaFormatter.SetKey(rsa);
                Signature = rsaFormatter.CreateSignature(hashmes);
            }
        }

        public IProtocolMessages CreateCopyTemplate() => new NewView(NewViewNr, ViewProof, PrePrepMessages);
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(NewViewNr),NewViewNr);
            stateToSerialize.Set(nameof(ViewProof), ViewProof);
            stateToSerialize.Set(nameof(PrePrepMessages), PrePrepMessages);
            stateToSerialize.Set(nameof(Signature), Signature);
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