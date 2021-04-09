using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
using Newtonsoft.Json.Serialization;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Tests.Helper;

namespace PBFT.Messages
{
    public class ViewChange : IProtocolMessages, SignedMessage, IPersistable
    {
        public int StableSeqNr { get; set; }
        public int ServID { get; set; }
        public int NextViewNr { get; set; }
        public CheckpointCertificate CertProof { get; set; }
        
        public CDictionary<int, ProtocolCertificate> RemPreProofs { get; set;}
        
        public Dictionary<int, ProtocolCertificate> TestRemProofs { get; set; }
        
        public byte[] Signature { get; set; }

        public ViewChange(int stableSeq, int rid, int newViewNr, CheckpointCertificate cProof, CDictionary<int, ProtocolCertificate> prepCerts) //update when you have an understanding of proofs
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProof = cProof;
            RemPreProofs = prepCerts;
        }

        [JsonConstructor]
        public ViewChange(int stableSeq, int rid, int newViewNr, CheckpointCertificate cProof, Dictionary<int, ProtocolCertificate> copyprepCerts)
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProof = cProof;
            RemPreProofs = new CDictionary<int, ProtocolCertificate>();
            foreach (var (key, val) in copyprepCerts)
                RemPreProofs[key] = val;
        }
        
        
        public ViewChange(int stableSeq, int rid, int newViewNr, CheckpointCertificate cproof, CDictionary<int, ProtocolCertificate> prepcerts, byte[] sign) //update when you have an understaning of proofs
        {
            StableSeqNr = stableSeq;
            ServID = rid;
            NextViewNr = newViewNr;
            CertProof = cproof;
            RemPreProofs = prepcerts;
            Signature = sign;
        }
        
        public byte[] SerializeToBuffer()
        {
            /*var copy = (ViewChange) CreateCopyTemplate();
            if (copy.CertProof != null) CertProof.EmitCheckpoint = null;
            var testdict = new Dictionary<int, ProtocolCertificate>();
            foreach (var (key, val) in RemPreProofs)
            {
                testdict[key] = val;
            }

            copy.TestRemProofs = testdict;
            copy.RemPreProofs = null;
            string jsonval = JsonConvert.SerializeObject(copy, Formatting.Indented);
            return Encoding.ASCII.GetBytes(jsonval);*/
            var copy = (ViewChange) CreateCopyTemplate();
            if (copy.CertProof != null) CertProof.EmitCheckpoint = null;
            var testdict = new Dictionary<int, ProtocolCertificate>();
            foreach (var (key, val) in RemPreProofs)
            {
                testdict[key] = val;
            }

            //var jsonform = new ViewChangeJsonFormatter(copy.NextViewNr, copy.ServID, copy.StableSeqNr, copy.CertProof, testdict, copy.Signature);
            //string jsonval = JsonConvert.SerializeObject(jsonform);
            string jsonval = JsonConvert.SerializeObject(copy);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static ViewChange DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
            Console.WriteLine(jsonobj);
            var test = JsonConvert.DeserializeObject<ViewChangeJsonFormatter>(jsonobj);
            Console.WriteLine("Got passed Test");
            int seq;
            int servid;
            int newvinr;
            var obj = jsonobj.Split("\n");
            for (var i = 0; i < obj.Length; i++)
            {
                Console.WriteLine(i);
                Console.WriteLine(obj[i]);
            }
            return new ViewChange(1, 1, 1, null, new CDictionary<int, ProtocolCertificate>());
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
            new ViewChange(StableSeqNr, ServID, NextViewNr, CertProof, RemPreProofs);

        public override string ToString()
        {
            string tostring = $"ServerID:{ServID}, NextViewNr:{NextViewNr} ,StableSeq:{StableSeqNr}\nProof:";
            foreach (var cproof in CertProof.ProofList) 
                tostring += $"ID: {cproof.ServID}, SeqNr:{cproof.StableSeqNr}\n";
            foreach (var (_,pproof) in RemPreProofs)
                tostring += $"SeqNr: {pproof.SeqNr}, ViewNr:{pproof.ViewNr}, CType:{pproof.CType}, RequestDigest:{pproof.CurReqDigest}\n";
            return tostring;
        }

        public bool Compare(ViewChange vc2)
        {
            if (vc2.StableSeqNr != StableSeqNr) return false;
            if (vc2.ServID != ServID) return false;
            if (vc2.NextViewNr != NextViewNr) return false;
            if (vc2.CertProof == null && CertProof != null || vc2.CertProof != null && CertProof == null) return false;
            if (vc2.CertProof != null && CertProof != null)
            {
                if (vc2.CertProof.LastSeqNr != CertProof.LastSeqNr) return false;
                if (!vc2.CertProof.StateDigest.SequenceEqual(CertProof.StateDigest)) return false;
                if (vc2.CertProof.ProofList.Count != CertProof.ProofList.Count) return false;     
            }
            if (vc2.Signature == null && Signature != null || vc2.Signature != null && Signature == null) return false;
            if (vc2.Signature != null && Signature != null && !vc2.Signature.SequenceEqual(Signature)) return false;
            if (vc2.RemPreProofs.Count != RemPreProofs.Count) return false;
            foreach (var (key, precert) in vc2.RemPreProofs)
            {
                if (!RemPreProofs.ContainsKey(key)) return false;
                var remcert = RemPreProofs[key];
                if (remcert.SeqNr != precert.SeqNr) return false;
                if (remcert.ViewNr != precert.ViewNr) return false;
                if (!remcert.CurReqDigest.SequenceEqual(precert.CurReqDigest)) return false;
                if (remcert.ProofList.Count != precert.ProofList.Count) return false;
            }
            return true;
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(StableSeqNr), StableSeqNr);
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(NextViewNr), NextViewNr);
            stateToSerialize.Set(nameof(CertProof), CertProof);
            stateToSerialize.Set(nameof(RemPreProofs), RemPreProofs);
            stateToSerialize.Set(nameof(Signature), Serializer.SerializeHash(Signature));
        }
        
        private static ViewChange Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ViewChange(sd.Get<int>(nameof(StableSeqNr)),
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(NextViewNr)),
                sd.Get<CheckpointCertificate>(nameof(CertProof)),
                sd.Get<CDictionary<int, ProtocolCertificate>>(nameof(RemPreProofs)),
                Deserializer.DeserializeHash(sd.Get<string>(nameof(Signature)))
            );
    }
}