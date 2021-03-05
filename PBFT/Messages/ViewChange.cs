using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace PBFT.Messages
{
    public class ViewChange : IProtocolMessages, SignedMessage
    {
        public int ServID;
        public int NextViewNr;
        //public int StableSequenceNr (based on checkpoints)
        //Proof of last Checkpoint
        
        public byte[] Signature;

        public ViewChange(int rid, int newViewNr) //update when you have an understanding of proofs
        {
            ServID = rid;
            NextViewNr = newViewNr;
        }

        [JsonConstructor]
        public ViewChange(int rid, int newViewNr, byte[] sign) //update when you have an understaning of proofs
        {
            ServID = rid;
            NextViewNr = newViewNr;
            Signature = sign;
        }
        
        public byte[] SerializeToBuffer()
        {
            throw new System.NotImplementedException();
        }

        public static ViewChange DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
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
                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                rsaFormatter.SetHashAlgorithm(haspro);
                rsaFormatter.SetKey(rsa);
                Signature = rsaFormatter.CreateSignature(hashmes);
            }
        }

        public IProtocolMessages CreateCopyTemplate() =>
            new ViewChange(ServID, NextViewNr); //update when constructor is updated
    }
}