using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace PBFT.Messages
{
    public class ViewChange : IProtocolMessages, SignedMessage
    {
        public int ServID;
        public int nextViewNr;
        //public int StableSequenceNr (based on checkpoints)
        //Proof of last Checkpoint
        
        public byte[] Signature;

        public byte[] SerializeToBuffer()
        {
            throw new System.NotImplementedException();
        }

        public static ViewChange DeSerializeToObject(byte[] buffer)
        {
            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<ViewChange>(jsonobj);
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
                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                RSAFormatter.SetHashAlgorithm(haspro);
                RSAFormatter.SetKey(rsa);
                Signature = RSAFormatter.CreateSignature(hashmes);
            }
        }

        public IProtocolMessages CreateCopyTemplate()
        {
            throw new System.NotImplementedException();
        }
    }
}