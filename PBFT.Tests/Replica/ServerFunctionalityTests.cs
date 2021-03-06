using System;
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
            Server testserv = new Server(1,1,4,null,50,"127.0.0.1:9001", new Source<Request>(),new Source<PhaseMessage>(), new CDictionary<int, string>());
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
            //var (_prikey, pubkey) = Crypto.InitializeKeyPairs();
            var server = new Server(0,0,4,null,20,"127.0.0.1:9000", null, null, new CDictionary<int, string>());
            
        }
    }
}