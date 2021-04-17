using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Checkpoint : IProtocolMessages, ISignedMessage, IPersistable
    {
        public int ServID { get; set; }
        public int StableSeqNr{ get; set; }
        public byte[] StateDigest { get; set; } //Digest of the state
        public byte[] Signature{ get; set; }
        
        public Checkpoint(int id, int seqnr, byte[] statedigest)
        {
            ServID = id;
            StableSeqNr = seqnr;
            StateDigest = statedigest;
        }

        [JsonConstructor]
        public Checkpoint(int id, int seqnr, byte[] statedigest, byte[] sign)
        {
            ServID = id;
            StableSeqNr = seqnr;
            StateDigest = statedigest;
            Signature = sign;
        }

        public byte[] SerializeToBuffer()
        {
            var jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }
        
        public static Checkpoint DeSerializeToObject(byte[] buffer)
        {
            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<Checkpoint>(jsonobj);
        }
        
        public void SignMessage(RSAParameters prikey, string haspro = "SHA256")
        {
            using (var rsa = RSA.Create())
            {
                byte[] hashmes;
                using (var shaalgo = SHA256.Create())
                {
                    var serareq = SerializeToBuffer();
                    hashmes = shaalgo.ComputeHash(serareq);
                }
                rsa.ImportParameters(prikey);
                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(); //https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsapkcs1signatureformatter?view=net-5.0
                rsaFormatter.SetHashAlgorithm(haspro);
                rsaFormatter.SetKey(rsa);
                Signature = rsaFormatter.CreateSignature(hashmes);
            }
        }

        public bool Validate(RSAParameters pubkey)
        {
            if (StableSeqNr < 0) return false;
            if (StateDigest == null) return false;
            var clone = (Checkpoint) CreateCopyTemplate();
            if (Signature == null || !Crypto.VerifySignature(Signature, clone.SerializeToBuffer(), pubkey))
                return false;
            return true;
        }
        
        public IProtocolMessages CreateCopyTemplate() => new Checkpoint(ServID, StableSeqNr, StateDigest);

        public override string ToString()
        {
            string tostring = $"ServID: {ServID}, SeqNumber: {StableSeqNr},";
            if (StateDigest != null) tostring += $"Digest: {BitConverter.ToString(StateDigest)},";
            tostring += $"Signature: {Signature}";
            return tostring;
        }
        
        public bool Compare(Checkpoint check)
        {
            if (check.ServID != ServID) return false;
            if (check.StableSeqNr != StableSeqNr) return false;
            if (check.StateDigest == null && StateDigest != null || check.StateDigest != null && StateDigest == null) return false;
            if (check.StateDigest != null && StateDigest != null && !check.StateDigest.SequenceEqual(StateDigest)) return false;
            if (check.Signature == null && Signature != null || check.Signature != null && Signature == null) return false;
            if (check.Signature != null && Signature != null && !check.Signature.SequenceEqual(Signature)) return false;
            return true;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(StableSeqNr), StableSeqNr);
            stateToSerialize.Set(nameof(StateDigest), Serializer.SerializeHash(StateDigest));
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }

        private static Checkpoint Deserialize(IReadOnlyDictionary<string, object> sd)
            => new Checkpoint(sd.Get<int>(nameof(ServID)),
                              sd.Get<int>(nameof(StableSeqNr)),
                              Deserializer.DeserializeHash(sd.Get<string>(nameof(StateDigest))),
                              Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
                             );

    }
}