using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using Cleipnir.ExecutionEngine.DataStructures;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;
using PBFT.Helper;

namespace PBFT.Messages
{
    public class ViewChange : IProtocolMessages, SignedMessage, IPersistable
    {
        public int StableSeqNr { get; set; }
        public int ServID { get; set; }
        public int NextViewNr { get; set; }
        public CheckpointCertificate CertProofs { get; set; }
        
        public CDictionary<int, ProtocolCertificate> RemPreProofs { get; set;}
        
        public byte[] Signature { get; set; }

        public ViewChange(int stableSeq, int rid, int newViewNr, CheckpointCertificate cProof, CDictionary<int, ProtocolCertificate> prepCerts) //update when you have an understanding of proofs
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProofs = cProof;
            RemPreProofs = prepCerts;
        }

        [JsonConstructor]
        public ViewChange(int stableSeq, int rid, int newViewNr, CheckpointCertificate cproof, CDictionary<int, ProtocolCertificate> prepcerts, byte[] sign) //update when you have an understaning of proofs
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProofs = cproof;
            RemPreProofs = prepcerts;
            Signature = sign;
        }
        
        public byte[] SerializeToBuffer()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
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

        public bool Validate(RSAParameters pubkey, int nextview)
        {
            var copy = CreateCopyTemplate();
            if (nextview != NextViewNr) return false;
            if(!Crypto.VerifySignature(Signature,copy.SerializeToBuffer(), pubkey)) return false;
            //Verify Checkout Certificate... 
            return true;
        }

        public bool HasPrepares() => RemPreProofs.Count != 0;

        public IProtocolMessages CreateCopyTemplate() => 
            new ViewChange(StableSeqNr, ServID, NextViewNr, CertProofs, RemPreProofs);

        public override string ToString()
        {
            string tostring = $"ServerID:{ServID}, NextViewNr:{NextViewNr} ,StableSeq:{StableSeqNr}\nProof:";
            foreach (var cproof in CertProofs.ProofList) 
                tostring += $"ID: {cproof.ServID}, SeqNr:{cproof.StableSeqNr}\n";
            foreach (var (_,pproof) in RemPreProofs)
                tostring += $"SeqNr: {pproof.SeqNr}, ViewNr:{pproof.ViewNr}, CType:{pproof.CType}, RequestDigest:{pproof.CurReqDigest}\n";
            return tostring;
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(StableSeqNr), StableSeqNr);
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(NextViewNr), NextViewNr);
            stateToSerialize.Set(nameof(CertProofs), CertProofs);
            stateToSerialize.Set(nameof(RemPreProofs), RemPreProofs);
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }
        
        private static ViewChange Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewChange(sd.Get<int>(nameof(StableSeqNr)),
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(NextViewNr)),
                sd.Get<Certificates.CheckpointCertificate>(nameof(CertProofs)),
                sd.Get<CDictionary<int,ProtocolCertificate>>(nameof(RemPreProofs)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
            );

    }
}