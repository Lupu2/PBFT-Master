using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Replica
{
    [TestClass]
    public class ServerFunctionalityTests
    {
        [TestMethod]
        public void ChangeClientTest()
        {
            var sh = new SourceHandler(new Source<Request>(), new Source<PhaseMessage>(), null, null, null, null);
            Server testserv = new Server(1,1,4,null,50,"127.0.0.1:9001", sh, new CDictionary<int, string>());
            testserv.ClientActive[1] = false;
            testserv.ClientActive[2] = false;
            Assert.IsFalse(testserv.ClientActive[1]);
            Assert.IsFalse(testserv.ClientActive[2]);
            testserv.ChangeClientStatus(1);
            Assert.IsTrue(testserv.ClientActive[1]);
            Assert.IsFalse(testserv.ClientActive[2]);
            testserv.ChangeClientStatus(2);
            Assert.IsTrue(testserv.ClientActive[1]);
            Assert.IsTrue(testserv.ClientActive[2]);
            testserv.ChangeClientStatus(1);
            Assert.IsFalse(testserv.ClientActive[1]);
            Assert.IsTrue(testserv.ClientActive[2]);
            testserv.ChangeClientStatus(2);
            Assert.IsFalse(testserv.ClientActive[1]);
            Assert.IsFalse(testserv.ClientActive[2]);
            testserv.ChangeClientStatus(1);
            testserv.ChangeClientStatus(2);
            Assert.IsTrue(testserv.ClientActive[1]);
            Assert.IsTrue(testserv.ClientActive[2]);
        }

        [TestMethod]
        public void ServerSigningPhaseMessageTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            var sh = new SourceHandler(null, null, null, null, null, null);
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            var req = new Request(1, "op");
            var digest = Crypto.CreateDigest(req);
            req.SignMessage(_prikey);
            var phasemes = new PhaseMessage(0, 1, 1, digest, PMessageType.Prepare);
            Assert.IsFalse(Crypto.VerifySignature(phasemes.Signature,phasemes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            server.SignMessage(phasemes, MessageType.PhaseMessage);
            Assert.IsTrue(Crypto.VerifySignature(phasemes.Signature, phasemes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }

        [TestMethod]
        public void ServerSigningReplyTest()
        {
            var sh = new SourceHandler(null, null, null, null, null, null);
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
           
            var replymes = new Reply(server.ServID, 1, server.CurView, true, "Result", DateTime.Now.ToString());
            Assert.IsFalse(Crypto.VerifySignature(replymes.Signature,replymes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            server.SignMessage(replymes, MessageType.Reply);
            Assert.IsTrue(Crypto.VerifySignature(replymes.Signature, replymes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }
        
        [TestMethod]
        public void ServerSigningViewChangeTest()
        {
            var sh = new SourceHandler(null, null, null, null, null, null); 
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
           
            var viewmes = new ViewChange(0, 0, 1, null, null);
            Assert.IsFalse(Crypto.VerifySignature(viewmes.Signature,viewmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            server.SignMessage(viewmes, MessageType.ViewChange);
            Assert.IsTrue(Crypto.VerifySignature(viewmes.Signature, viewmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }

        [TestMethod]
        public void ServerSigningNewViewTest()
        {
            var sh = new SourceHandler(null, null, null, null, null, null); 
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            
            var newviewmes = new NewView(1, null, null);
            Assert.IsFalse(Crypto.VerifySignature(newviewmes.Signature,newviewmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            server.SignMessage(newviewmes, MessageType.NewView);
            Assert.IsTrue(Crypto.VerifySignature(newviewmes.Signature, newviewmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }
        
        [TestMethod]
        public void ServerSigningCheckpointTest()
        {
            var sh = new SourceHandler(null, null, null, null, null, null);
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            var checkmes = new Checkpoint(server.ServID, 20, null);
            Assert.IsFalse(Crypto.VerifySignature(checkmes.Signature,checkmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            server.SignMessage(checkmes, MessageType.Checkpoint);
            Assert.IsTrue(Crypto.VerifySignature(checkmes.Signature, checkmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }
        
        [TestMethod]
        public void ServerCollectPrepareCertificatesTest()
        {
            var server = new Server(0, 1, 5, 4, null, 10, "127.0.0.1:9001", null, new CDictionary<int, string>());
            server.InitializeLog(0);
            server.InitializeLog(1);
            server.InitializeLog(2);
            server.InitializeLog(3);
            server.InitializeLog(4);
            server.InitializeLog(5);
            var dig0 = Crypto.CreateDigest(new Request(1, "hello", "12:00"));
            var dig1 = Crypto.CreateDigest(new Request(2, "Pops", "12:01"));
            var dig2 = Crypto.CreateDigest(new Request(3, "Smart", "12:02"));
            var dig3 = Crypto.CreateDigest(new Request(1, "Jason", "12:03"));
            var dig4 = Crypto.CreateDigest(new Request(2, "Jake", "12:04"));
            var dig5 = Crypto.CreateDigest(new Request(3, "Lake", "12:05"));
            var propcert0 = new ProtocolCertificate(0, 1, dig0, CertType.Prepared);
            var comcert0 = new ProtocolCertificate(0, 1, dig0, CertType.Committed);
            var propcert1 = new ProtocolCertificate(1, 1, dig1, CertType.Prepared);
            var comcert1 = new ProtocolCertificate(1, 1, dig1, CertType.Committed);
            var propcert2 = new ProtocolCertificate(2, 1, dig2, CertType.Prepared);
            var comcert2 = new ProtocolCertificate(2, 1, dig2, CertType.Committed);
            var propcert3 = new ProtocolCertificate(3, 1, dig3, CertType.Prepared);
            var comcert3 = new ProtocolCertificate(3, 1, dig3, CertType.Committed);
            var propcert4 = new ProtocolCertificate(4, 1, dig4, CertType.Prepared);
            var comcert4 = new ProtocolCertificate(4, 1, dig4, CertType.Committed);
            var propcert5 = new ProtocolCertificate(5, 1, dig5, CertType.Prepared);
            List<ProtocolCertificate> protoList = new List<ProtocolCertificate>() 
            {
                propcert1, 
                propcert2, 
                propcert3, 
                propcert4,
                propcert5
            };
            server.AddProtocolCertificate(0, propcert0);
            server.AddProtocolCertificate(0, comcert0);
            server.AddProtocolCertificate(1, propcert1);
            server.AddProtocolCertificate(1, comcert1);
            server.AddProtocolCertificate(2, propcert2);
            server.AddProtocolCertificate(2, comcert2);
            server.AddProtocolCertificate(3, propcert3);
            server.AddProtocolCertificate(3, comcert3);
            server.AddProtocolCertificate(4, propcert4);
            server.AddProtocolCertificate(4, comcert4);
            server.AddProtocolCertificate(5, propcert5);
            var resdict = server.CollectPrepareCertificates(0);
            Assert.AreEqual(resdict.Count, 5);
            foreach (var (i,procert) in resdict)
            {
                Assert.IsTrue(procert.CType == CertType.Prepared);
                Assert.AreEqual(procert.SeqNr,protoList[i-1].SeqNr);
                Assert.AreEqual(procert.ViewNr, protoList[i-1].ViewNr);
                Assert.IsTrue(procert.CurReqDigest.SequenceEqual(protoList[i-1].CurReqDigest));
            }
        }
    }
}