using Newtonsoft.Json; //Replace Newtonsoft.JSON with System.Text.Json it is faster apperently
using System;
using System.Text;
using System.Security.Cryptography;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using PBFT.Helper;

namespace PBFT.ProtocolMessages
{
    public enum MessageType 
    {
        PrePrepare,
        Prepare,
        Commit,
    }

    public class PhaseMessage : IProtocolMessages<PhaseMessage>, IPersistable
    {
        public int ServID {get;set;}

        public int SeqNr {get;set;}
        
        public int ViewNr{get;set;}
        public byte[] Digest{get;set;}
        
        public byte[] Signature{get;set;}

        public MessageType Type{get;set;}

        public PhaseMessage(int id,int seq,int view, byte[] dig, MessageType phase)
        {
            ServID = id;
            SeqNr = seq;
            ViewNr = view;
            Digest = dig;
            Type = phase;
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
            stateToSerialize.Set(nameof(Digest), Digest);
            stateToSerialize.Set(nameof(Signature), Signature);
            stateToSerialize.Set(nameof(Type), Type);
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

        public bool Validate(RSAParameters pubkey,int cviewNr, int seqLow,int seqHigh)
        {
            bool valid = true;
            var clone = CreateCopyTemplate(this);
            if (!Crypto.VerifySignature(Signature, clone.SerializeToBuffer(), pubkey)) return false;
            if (ViewNr != cviewNr) return false;
            if (SeqNr > seqLow || SeqNr > seqHigh) return false;
            if(Type == MessageType.PrePrepare) Console.WriteLine("Extra check!"); //check if already exist a stored prepare with seqnr = to this message
            return valid;
        }

        private PhaseMessage CreateCopyTemplate(PhaseMessage org) => new PhaseMessage(org.ServID,org.SeqNr,org.ViewNr,org.Digest,org.Type);
    }
}