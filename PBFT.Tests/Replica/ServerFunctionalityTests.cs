using System;
using System.Dynamic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Server testserv = new Server(1,1,4,null,50,"127.0.0.1:9001", 
                new Source<Request>(),new Source<PhaseMessage>(), null, null, null,
                new CDictionary<int, string>());
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
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", 
                null, null, null, null, null, new CDictionary<int, string>());
            var req = new Request(1, "op");
            var digest = Crypto.CreateDigest(req);
            req.SignMessage(_prikey);
            var phasemes = new PhaseMessage(0, 1, 1, digest, PMessageType.Prepare);
            Assert.IsFalse(Crypto.VerifySignature(phasemes.Signature,phasemes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            phasemes = (PhaseMessage) server.SignMessage(phasemes, MessageType.PhaseMessage);
            Assert.IsTrue(Crypto.VerifySignature(phasemes.Signature, phasemes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }

        [TestMethod]
        public void ServerSigningReplyTest()
        {
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", 
                null, null, null,null, null, new CDictionary<int, string>());
           
            var replymes = new Reply(server.ServID, 1, server.CurView, true, "Result", DateTime.Now.ToString());
            Assert.IsFalse(Crypto.VerifySignature(replymes.Signature,replymes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            replymes = (Reply) server.SignMessage(replymes, MessageType.Reply);
            Assert.IsTrue(Crypto.VerifySignature(replymes.Signature, replymes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }
        
        //TODO Write additional ServerSigning test methods once the other messages types are implemented
        [TestMethod]
        public void ServerSigningViewChangeTest()
        {
            
        }

        [TestMethod]
        public void ServerSigningNewViewTest()
        {
            
        }
        
        [TestMethod]
        public void ServerSigningCheckpointTest()
        {
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", 
                null, null, null, null, null, new CDictionary<int, string>());
           
            var checkmes = new Checkpoint(server.ServID, 20, null);
            Assert.IsFalse(Crypto.VerifySignature(checkmes.Signature,checkmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
            checkmes = (Checkpoint) server.SignMessage(checkmes, MessageType.Checkpoint);
            Assert.IsTrue(Crypto.VerifySignature(checkmes.Signature, checkmes.CreateCopyTemplate().SerializeToBuffer(), server.Pubkey));
        }
    }
}