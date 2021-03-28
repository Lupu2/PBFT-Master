using System;
using System.Security.Cryptography;
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
    }
}