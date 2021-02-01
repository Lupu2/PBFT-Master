using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace PBFT.Messages
{
    public class Reply : IProtocolMessages, SignedMessage
    {  
        public int ServID{get; set;}
        public int SeqNr{get; set;}
        public int ViewNr{get; set;}
        public bool Status{get; set;}
        public string Result {get; set;} //might be changed to object later
        public string Timestamp{get; set;}
        public byte[] Signature{get;set;}


        public Reply(int id, int seqnr, int vnr, bool success, string res, string timestamp)
        {
            ServID = id;
            SeqNr = seqnr;
            ViewNr = vnr;
            Status = success;
            Result = res;
            Timestamp = timestamp;
        }

        [JsonConstructor]
        public Reply(int id, int seqnr, int vnr, bool success, string res, string timestamp, byte[] sign)
        {
            ServID = id;
            SeqNr = seqnr;
            ViewNr = vnr;
            Status = success;
            Result = res;
            Timestamp = timestamp;
            Signature = sign;
        }

        public byte[] SerializeToBuffer()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static Reply DeSerializeToObject(byte[] buffer)
        {
            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<Reply>(jsonobj);
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

        public override string ToString() => $"ID: {ServID}, SequenceNr: {SeqNr}, CurrentView: {ViewNr}, Time:{Timestamp}, Status: {Status}, Result: {Result}, Sign:{Signature}";

        public IProtocolMessages CreateCopyTemplate() =>  new Reply(ServID, SeqNr, ViewNr, Status, Result, Timestamp, Signature);
        
    }
}