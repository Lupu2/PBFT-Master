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
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Helper;
using PBFT.Certificates;


namespace PBFT.Messages
{

    public class PhaseMessage : IProtocolMessages, ISignedMessage, IPersistable
    {
        public int ServID { get; set; }
        public int SeqNr { get; set; }
        public int ViewNr{ get; set; }
        public byte[] Digest{ get; set; }
        public byte[] Signature{ get; set; }
        public PMessageType PhaseType{ get; set; }

        public PhaseMessage(int id, int seq, int view, byte[] dig, PMessageType phase)
        {
            ServID = id;
            SeqNr = seq;
            ViewNr = view;
            Digest = dig;
            PhaseType = phase;
        }

        [JsonConstructor]
        public PhaseMessage(int id, int seq, int view, byte[] dig, PMessageType phase, byte[] sign)
        {
            ServID = id;
            SeqNr = seq;
            ViewNr = view;
            Digest = dig;
            PhaseType = phase;
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
            //Console.WriteLine("JSON OBJECT:");
            //Console.WriteLine(BitConverter.ToString(buffer));
            //Console.WriteLine(jsonobj);
            return JsonConvert.DeserializeObject<PhaseMessage>(jsonobj);
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

        public bool Validate(RSAParameters pubkey, int cviewNr, Range curSeqInterval, ProtocolCertificate cert = null)
        {
            //try
            //{
                Console.WriteLine($"VALIDATING PhaseMes {ServID} {PhaseType}");
                int seqLow = curSeqInterval.Start.Value;
                int seqHigh = curSeqInterval.End.Value;
                var clone = (PhaseMessage) CreateCopyTemplate();
                if (Signature == null || !Crypto.VerifySignature(Signature, clone.SerializeToBuffer(), pubkey))
                    return false;
                if (ViewNr != cviewNr) return false;
                Console.WriteLine("Passed view check");
                if (SeqNr < seqLow || SeqNr > seqHigh) return false;
                Console.WriteLine("Passed Range check");
                if (cert != null && cert.ProofList.Count > 0)
                    if (PhaseType == PMessageType.PrePrepare
                    ) //check if already exist a stored prepare with seqnr = to this message
                    {
                        foreach (var proof in cert.ProofList)
                        {
                            Console.WriteLine(proof.Digest.SequenceEqual(Digest));
                            if (proof.PhaseType == PMessageType.PrePrepare && proof.SeqNr == SeqNr &&
                                !proof.Digest.SequenceEqual(Digest))
                                return false; //should usually be the first entry in the list
                        }
                    }
                Console.WriteLine($"PhaseMes Validation {ServID},{PhaseType} True");
                return true;
            //}
            //catch (Exception e)
            //{
                //Console.WriteLine("Error in Validate (PhaseMessage)");
                //Console.WriteLine(e);
                //throw;
            //}
        }
        
        public bool ValidateRedo(RSAParameters pubkey, int cviewNr)
        {
            Console.WriteLine($"VALIDATING PhaseMes {ServID} {PhaseType}");
            var clone = CreateCopyTemplate();
            if (Signature == null || !Crypto.VerifySignature(Signature, clone.SerializeToBuffer(), pubkey))
                return false;
            if (ViewNr != cviewNr) return false;
            Console.WriteLine($"PhaseMes Validation {ServID},{PhaseType} True");
            return true;
        }

        public IProtocolMessages CreateCopyTemplate() => new PhaseMessage(ServID, SeqNr, ViewNr, Digest, PhaseType);

        public override string ToString()
        {
            if (Digest != null)
                return$"ID:{ServID}, SeqNr: {SeqNr}, ViewNr: {ViewNr}, Phase: {PhaseType} \nDigest: {BitConverter.ToString(Digest)}\n Signature: {Signature}";
            return$"ID:{ServID}, SeqNr: {SeqNr}, ViewNr: {ViewNr}, Phase: {PhaseType} \nDigest: {null}\n Signature: {Signature}";
        }
        
        public bool Compare(PhaseMessage pes2)
        {
            if (pes2.ServID != ServID) return false;
            if (pes2.SeqNr != SeqNr) return false;
            if (pes2.ViewNr != ViewNr) return false;
            if (pes2.PhaseType != PhaseType) return false;
            if (pes2.Digest == null && Digest != null || pes2.Digest != null && Digest == null) return false;
            if (pes2.Digest != null && Digest != null && !pes2.Digest.SequenceEqual(Digest)) return false;
            if (pes2.Signature == null && Signature != null || pes2.Signature != null && Signature == null) return false;
            if (pes2.Signature != null && Signature != null && !pes2.Signature.SequenceEqual(Signature)) return false;
            return true;
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(SeqNr), SeqNr);
            stateToSerialize.Set(nameof(ViewNr), ViewNr);
            stateToSerialize.Set(nameof(Digest), Serializer.SerializeHash(Digest));
            stateToSerialize.Set(nameof(PhaseType), (int)PhaseType);
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }

        private static PhaseMessage Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new PhaseMessage(
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(SeqNr)),
                sd.Get<int>(nameof(ViewNr)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Digest))),
                Enums.ToEnumPMessageType(sd.Get<int>(nameof(PhaseType))),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
            );
        }
    }
}