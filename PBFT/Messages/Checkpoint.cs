using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace PBFT.Messages
{
    public class Checkpoint : IProtocolMessages, SignedMessage
    {
        public int ServID {get; set;}
        public int SeqNr{get; set;}
        public byte[] Digest {get; set;} //Digest of the state
        public byte[] Signature{get; set;}
        public byte[] SerializeToBuffer()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

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
                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                RSAFormatter.SetHashAlgorithm(haspro);
                RSAFormatter.SetKey(rsa);
                Signature = RSAFormatter.CreateSignature(hashmes);
            }
        }
    }
}