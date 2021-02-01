using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;

namespace PBFT.ProtocolMessages
{
    public class Request : IProtocolMessages<Request>, IPersistable 
    {
        public int ClientID {get; set;}
        public string Message{get; set;} //operation might be changed to object later on
        public string Timestamp{get; set;}
        public byte[] Signature{get; set;}
        //Potentially add a t value that increments after each signature in order to avoid replay attacks...
        public Request(int id, string op, string time)
        {
            ClientID = id;
            Message = op;
            Timestamp = time;
        }

        [JsonConstructor]
        public Request(int id, string op, string time, byte[] sign)
        {
            ClientID = id;
            Message = op;
            Timestamp = time;
            Signature = sign;
        }
        public byte[] SerializeToBuffer()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static Request DeSerializeToObject(byte[] buffer)
        {
            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<Request>(jsonobj);
        }
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ClientID), ClientID);
            stateToSerialize.Set(nameof(Message), Message);
            stateToSerialize.Set(nameof(Timestamp), Timestamp);
            stateToSerialize.Set(nameof(Signature), Signature); //might have to use ListSerializer for this.
        }

        public static Request Deserialize(IReadOnlyDictionary<string, object> sd) 
        => new Request((int) sd[nameof(ClientID)], (string) sd[nameof(Message)], (string) sd[nameof(Timestamp)], (byte[]) sd[nameof(Signature)]);

        public void SignMessage(RSAParameters prikey, string haspro = "SHA256")
        {
            using (var rsa = RSA.Create())
            {
                byte[] hashmes;
                using (var shaalgo = SHA256.Create())
                {
                    var serareq = this.SerializeToBuffer();
                    hashmes = shaalgo.ComputeHash(serareq);
                    Console.WriteLine("Hash1");
                    Console.WriteLine(BitConverter.ToString(hashmes));
                }
                rsa.ImportParameters(prikey);
                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                RSAFormatter.SetHashAlgorithm(haspro);
                RSAFormatter.SetKey(rsa);
                Signature = RSAFormatter.CreateSignature(hashmes);
            }
        }

        public override string ToString() => $"ID: {ClientID}, Message: {Message}, Time:{Timestamp}, Sign:{Signature}";
    }

}