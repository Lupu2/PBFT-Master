using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Certificates;

namespace PBFT.Tests
{
    [TestClass]
    public class MessageValidationTests
    {
        [TestMethod]
        public void ValidatePhaseMessageTest()
        {   
            //Normal Prepare test
            RSAParameters _prikey1;
            RSAParameters Pubkey1;
            RSAParameters _prikey2;
            RSAParameters Pubkey2;
            using (RSA rsa = RSA.Create())
            {
                _prikey1 = rsa.ExportParameters(true);
                Pubkey1 = rsa.ExportParameters(false);
            }

            using (RSA rsa = RSA.Create())
            {
                _prikey2 = rsa.ExportParameters(true);
                Pubkey2 = rsa.ExportParameters(false);
            }
            
            Request req = new Request(1, "Hello World", DateTime.Now.ToString());
            byte[] digest = Crypto.CreateDigest(req);
            Range ran = new Range(1, 5);
            
            PhaseMessage pes = new PhaseMessage(2, 1, 1, digest, PMessageType.Prepare);
            pes.SignMessage(_prikey1);
            Assert.IsTrue(pes.Validate(Pubkey1, 1, ran));
            
            //No Signature
            PhaseMessage pes2 = new PhaseMessage(2, 1, 1, digest, PMessageType.Prepare);
            Assert.IsFalse(pes2.Validate(Pubkey1,1,ran));
            
            //Faulty Signature
            PhaseMessage pes3 = new PhaseMessage(2, 1, 1, digest, PMessageType.Prepare);
            pes3.SignMessage(_prikey2);
            Assert.IsFalse(pes3.Validate(Pubkey1, 1, ran));
            
            //OutofBounds SeqNr 
            PhaseMessage pes4 = new PhaseMessage(2, 10, 1, digest, PMessageType.Prepare);
            pes4.SignMessage(_prikey1);
            Assert.IsFalse(pes4.Validate(Pubkey1,1,ran));
            
            //Not current viewnr
            PhaseMessage pes5 = new PhaseMessage(2, 1, 2, digest, PMessageType.Prepare);
            pes5.SignMessage(_prikey1);
            Assert.IsFalse(pes5.Validate(Pubkey1,1,ran));

            //Normal Commit
            PhaseMessage pes6 = new PhaseMessage(2, 1, 1, digest, PMessageType.Commit);
            pes6.SignMessage(_prikey2);
            Assert.IsTrue(pes6.Validate(Pubkey2, 1, ran));

            //Normal PrePrepare no existing Proofs
            PhaseMessage pes7 = new PhaseMessage(2, 1, 1, digest, PMessageType.PrePrepare);
            pes7.SignMessage(_prikey1);
            Assert.IsTrue(pes7.Validate(Pubkey1,1,ran));
            
            //Valid PrePrepare with existing Proofs
            CList<PhaseMessage> proofs = new CList<PhaseMessage>();
            proofs.Add(pes7);
            ProtocolCertificate q = new ProtocolCertificate(1, 2, digest, CertType.Prepared, false, proofs);
            PhaseMessage pes8 = new PhaseMessage(2, 1, 1, digest, PMessageType.PrePrepare);
            pes8.SignMessage(_prikey2);
            Assert.IsTrue(pes8.Validate(Pubkey2,1,ran,q));
            
            //Valid PrePrepare but other valid PrePrepare is there with other digest
            Request req2 = new Request(1, "Hello World!", DateTime.Now.ToString()); //will be different do to DateTime.Now
            byte[] digest2 = Crypto.CreateDigest(req2);
            PhaseMessage pes9 = new PhaseMessage(2, 1, 1, digest2, PMessageType.PrePrepare);
            pes9.SignMessage(_prikey1);
            Assert.IsFalse(pes9.Validate(Pubkey1,1,ran,q));
        }

        [TestMethod]
        public void ValidationReplyMessageTest()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            string createtime = DateTime.Now.ToString();
            Thread.Sleep(1000);
            string cretetime2 = DateTime.Now.ToString();
            var orgreq1 = new Request(1, "Hello", createtime);
            var orgreq2 = new Request(2, "Mark", cretetime2);
            orgreq1.SignMessage(pri);
            orgreq2.SignMessage(pri2);

            var rep1 = new Reply(1, 1, 1, true, orgreq1.Message, createtime);
            rep1.SignMessage(pri);
            Assert.IsTrue(rep1.Validate(pub, orgreq1));
            Assert.IsFalse(rep1.Validate(pub2, orgreq1));
            Assert.IsFalse(rep1.Validate(pub, orgreq2));

            var rep2 = new Reply(1, 1, 1, true, orgreq2.Message, cretetime2);
            Assert.IsFalse(rep2.Validate(pub2, orgreq2));
            rep2.SignMessage(pri2);
            Assert.IsFalse(rep2.Validate(pub, orgreq1));
            Assert.IsFalse(rep2.Validate(pub2, orgreq1));
            Assert.IsTrue(rep2.Validate(pub2, orgreq2));
            Assert.IsFalse(rep2.Validate(pub, orgreq2));
        }
        
        [TestMethod]
        public void ValidateViewChangeMessageTest()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();

            var viewMes1 = new ViewChange(-1,1,1,null, new CDictionary<int, ProtocolCertificate>());
            viewMes1.SignMessage(pri);
            var viewMes2 = new ViewChange(-1, 1, 1, null, new CDictionary<int, ProtocolCertificate>());
            viewMes2.SignMessage(pri2);
            
            Assert.IsTrue(viewMes1.Validate(pub, 1));
            Assert.IsFalse(viewMes1.Validate(pub2,1));
            Assert.IsFalse(viewMes1.Validate(pub, 2));
            Assert.IsFalse(viewMes2.Validate(pub,1));
            Assert.IsTrue(viewMes2.Validate(pub2, 1));
            Assert.IsFalse(viewMes2.Validate(pub2,2));
        }

        [TestMethod]
        public void ValidateCheckpointMessageTest()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            var (pri3, pub3) = Crypto.InitializeKeyPairs();
            var dig = Crypto.CreateDigest(new Request(1, "hello", "12:00"));
            var checkmes1 = new Checkpoint(0, 2, dig);
            var checkmes2 = new Checkpoint(1,-1,dig);
            var checkmes3 = new Checkpoint(2, 1, null);
            Assert.IsFalse(checkmes1.Validate(pub));
            checkmes1.SignMessage(pri);
            Assert.IsTrue(checkmes1.Validate(pub));
            Assert.IsFalse(checkmes1.Validate(pub2));
            checkmes2.SignMessage(pri2);
            Assert.IsFalse(checkmes2.Validate(pub2));
            checkmes3.Validate(pri3);
            Assert.IsFalse(checkmes3.Validate(pub3));
        }
    }
}