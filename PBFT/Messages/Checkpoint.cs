using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using PBFT.Helper;

namespace PBFT.Messages
{
    public class Checkpoint : IProtocolMessages, SignedMessage, IPersistable
    {
        public int ServID {get; set;}
        public int SeqNr{get; set;}
        public byte[] Digest {get; set;} //Digest of the state
        public byte[] Signature{get; set;}
        

        public Checkpoint(int id, int seqnr, byte[] statedigest)
        {
            ServID = id;
            SeqNr = seqnr;
            Digest = statedigest;
        }

        [JsonConstructor]
        public Checkpoint(int id, int seqnr, byte[] statedigest, byte[] sign)
        {
            ServID = id;
            SeqNr = seqnr;
            Digest = statedigest;
            Signature = sign;
        }

        public byte[] SerializeToBuffer()
        {
            var jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }
        
        public IProtocolMessages CreateCopyTemplate() => new Checkpoint(ServID, SeqNr, Digest);

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

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(Digest), Serializer.SerializeHash(Digest));
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }

        private static Checkpoint Deserialize(IReadOnlyDictionary<string, object> sd)
            => new Checkpoint(sd.Get<int>(nameof(ServID)),
                              sd.Get<int>(nameof(SeqNr)),
                              Deserializer.DeserializeHash(sd.Get<string>(nameof(Digest))),
                              Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
                             );

    }
}