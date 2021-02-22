using System;
using System.Security.Cryptography;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Tests.Persistency
{
    [TestClass]
    public class MessagesTests
    {
        private InMemoryStorageEngine _storage;
        //private SimpleFileStorageEngine _storage;
        private ObjectStore _objectStore;
        private RSAParameters _pri;
        private RSAParameters _pub;
        
        [TestInitialize]
        public void InitializeStorage()
        {
            _storage = new InMemoryStorageEngine();
            //_storage = new SimpleFileStorageEngine("test.txt");
            _objectStore = ObjectStore.New(_storage);
            (_pri, _pub) = Crypto.InitializeKeyPairs();
        }
        
        [TestMethod]
        public void RequestTest()
        {
            var currentTime = DateTime.Now.ToString();
            Request req = new Request(1, "Hello World!", currentTime);
            req.SignMessage(_pri);
            Assert.AreEqual(req.ClientID,1);
            StringAssert.Contains(req.Message,"Hello World!");
            StringAssert.Contains(req.Timestamp,DateTime.Now.ToString()); //usually fast enough
            _objectStore.Attach(req);
            _objectStore.Persist();
            _objectStore = ObjectStore.Load(_storage, false);
            Request req2 = _objectStore.Resolve<Request>();
            Assert.IsTrue(req.Compare(req2));
        }
        
        [TestMethod]
        public void PhaseMessageTest()
        {
            Request re12 = new Request(1, "Hello George", DateTime.Now.ToString());
            PhaseMessage mes1 = new PhaseMessage(1, 1, 1, Crypto.CreateDigest(re12), PMessageType.PrePrepare);
            mes1.SignMessage(_pri);
            Assert.AreEqual(mes1.ServID,1);
            Assert.AreEqual(mes1.SeqNr,1);
            Assert.AreEqual(mes1.ViewNr,1);
            Assert.AreEqual(mes1.Type,PMessageType.PrePrepare);
            _objectStore.Attach(mes1);
            _objectStore.Persist();
            _objectStore = ObjectStore.Load(_storage, false);
            PhaseMessage copy1 = _objectStore.Resolve<PhaseMessage>();
            Assert.IsTrue(mes1.Compare(copy1));
        }

        [TestMethod]
        public void ReplyTest()
        {
            Reply rep = new Reply(1, 1, 1, true, "Hello World", DateTime.Now.ToString());
            rep.SignMessage(_pri);
            Assert.AreEqual(rep.ServID,1);
            Assert.AreEqual(rep.SeqNr,1);
            Assert.AreEqual(rep.ViewNr,1);
            Assert.IsTrue(rep.Status);
            StringAssert.Contains(rep.Result,"Hello World");
            StringAssert.Contains(rep.Timestamp, DateTime.Now.ToString());
            _objectStore.Attach(rep);
            _objectStore.Persist();
            _objectStore = ObjectStore.Load(_storage, false);
            Reply rep2 = _objectStore.Resolve<Reply>();
            Assert.IsTrue(rep.Compare(rep2));
        }
    }
}