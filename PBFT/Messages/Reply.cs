using System;
using System.Linq;
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
            var jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static Reply DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
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
                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                rsaFormatter.SetHashAlgorithm(haspro);
                rsaFormatter.SetKey(rsa);
                Signature = rsaFormatter.CreateSignature(hashmes);
            }
        }

        public override string ToString() => $"ID: {ServID}, SequenceNr: {SeqNr}, CurrentView: {ViewNr}, Time:{Timestamp}, Status: {Status}, Result: {Result}, Sign:{Signature}";

        public IProtocolMessages CreateCopyTemplate() =>  new Reply(ServID, SeqNr, ViewNr, Status, Result, Timestamp);

        public bool Compare(Reply rep)
        {
            if (rep.ServID != ServID) return false;
            if (rep.SeqNr != SeqNr) return false;
            if (rep.ViewNr != ViewNr) return false;
            if (rep.Status != Status) return false;
            if (!rep.Result.Equals(Result)) return false;
            if (!rep.Timestamp.Equals(Timestamp)) return false;
            if (rep.Signature == null && Signature != null || rep.Signature != null && Signature == null) return false;
            if (rep.Signature != null && Signature != null && !rep.Signature.SequenceEqual(Signature)) return false;
            return true;
        }
        
    }
}