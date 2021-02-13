using Newtonsoft.Json; //Replace Newtonsoft.JSON with System.Text.Json it is faster apperently
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using PBFT.Helper;
using PBFT.Replica;


namespace PBFT.Messages
{

    public class PhaseMessage : IProtocolMessages, SignedMessage, IPersistable
    {
        public int ServID {get; set;}

        public int SeqNr {get; set;}
        
        public int ViewNr{get; set;}
        public byte[] Digest{get; set;}
        
        public byte[] Signature{get; set;}

        public PMessageType Type{get; set;}

        public PhaseMessage(int id, int seq, int view, byte[] dig, PMessageType phase)
        {
            ServID = id;
            SeqNr = seq;
            ViewNr = view;
            Digest = dig;
            Type = phase;
        }

        [JsonConstructor]
        public PhaseMessage(int id, int seq, int view, byte[] dig, PMessageType phase, byte[] sign)
        {
            ServID = id;
            SeqNr = seq;
            ViewNr = view;
            Digest = dig;
            Type = phase;
            Signature = sign;
        }

        public byte[] SerializeToBuffer()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static PhaseMessage DeSerializeToObject(byte[] buffer)
        {
            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<PhaseMessage>(jsonobj);
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(Digest), Serializer.SerializeHash(Digest));
            stateToSerialize.Set(nameof(Type), (int)Type);
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }

        private static PhaseMessage Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new PhaseMessage(
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(SeqNr)),
                sd.Get<int>(nameof(ViewNr)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Digest))),
                Enums.ToEnumPMessageType(sd.Get<int>(nameof(Type))),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
                );
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

        public bool Validate(RSAParameters pubkey, int cviewNr, Range curSeqInterval, QCertificate cert = null)
        {
            int seqLow = curSeqInterval.Start.Value;
            int seqHigh = curSeqInterval.End.Value;
            var clone = CreateCopyTemplate();
            if (!Crypto.VerifySignature(Signature, clone.SerializeToBuffer(), pubkey)) return false;
            if (ViewNr != cviewNr) return false;
            if (SeqNr < seqLow || SeqNr > seqHigh) return false;
            if (cert != null && cert.ProofList.Count > 0)
                if (Type == PMessageType.PrePrepare) //check if already exist a stored prepare with seqnr = to this message
                {
                    foreach (var proof in cert.ProofList)
                    {
                        if (proof.Type == PMessageType.PrePrepare && proof.SeqNr == SeqNr && !proof.Digest.SequenceEqual(Digest)) return false; //should usually be the first entry in the list
                    }
                }
                    
            return true;
        }

        public IProtocolMessages CreateCopyTemplate() => new PhaseMessage(ServID, SeqNr, ViewNr, Digest, Type);
        
    }
}