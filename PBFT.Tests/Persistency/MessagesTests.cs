using System;
using System.Linq;
using System.Security.Cryptography;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.InMemory;
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

        [TestInitialize]
        public void InitializeStorage()
        {
            _storage = new InMemoryStorageEngine();
            //_storage = new SimpleFileStorageEngine("test.txt");
            _objectStore = ObjectStore.New(_storage);
            (_pri, _) = Crypto.InitializeKeyPairs();
        }
        
        [TestMethod]
        public void RequestTest()
        {
            var currentTime = DateTime.Now.ToString();
            Request req = new Request(1, "Hello World!", currentTime);
            req.SignMessage(_pri);
            Assert.AreEqual(req.ClientID,1);
            StringAssert.Contains(req.Message,"Hello World!");
            StringAssert.Contains(req.Timestamp,currentTime); //usually fast enough
            _objectStore.Attach(req);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage, false);
            Request req2 = _objectStore.Resolve<Request>();
            Assert.IsTrue(req.Compare(req2));
        }
        
        [TestMethod]
        public void PhaseMessageTest()
        {
            Request re12 = new Request(1, "Hello George", DateTime.Now.ToString());
            var dig = Crypto.CreateDigest(re12);
            PhaseMessage mes1 = new PhaseMessage(1, 1, 1, dig, PMessageType.PrePrepare);
            PhaseMessage mes2 = new PhaseMessage(2, 1, 1, dig, PMessageType.Prepare);
            PhaseMessage mes3 = new PhaseMessage(3, 1, 1, dig, PMessageType.Commit);
            mes1.SignMessage(_pri);
            mes2.SignMessage(_pri);
            mes3.SignMessage(_pri);
            
            Assert.AreEqual(mes1.ServID,1);
            Assert.AreEqual(mes1.SeqNr,1);
            Assert.AreEqual(mes1.ViewNr,1);
            Assert.AreEqual(mes1.PhaseType,PMessageType.PrePrepare);
            Assert.AreEqual(mes2.ServID,2);
            Assert.AreEqual(mes2.SeqNr,1);
            Assert.AreEqual(mes2.ViewNr,1);
            Assert.AreEqual(mes2.PhaseType,PMessageType.Prepare);
            Assert.AreEqual(mes3.ServID,3);
            Assert.AreEqual(mes3.SeqNr,1);
            Assert.AreEqual(mes3.ViewNr,1);
            Assert.AreEqual(mes3.PhaseType,PMessageType.Commit);

            _objectStore.Attach(mes1);
            _objectStore.Attach(mes2);
            _objectStore.Attach(mes3);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage, false);
            var copies = _objectStore.ResolveAll<PhaseMessage>();
            foreach (var copy in copies)
            {
                switch (copy.ServID)
                {
                    case 1:
                        Assert.IsTrue(copy.Compare(mes1));
                        Assert.IsFalse(copy.Compare(mes2));
                        Assert.IsFalse(copy.Compare(mes3));
                        break;
                    case 2:
                        Assert.IsFalse(copy.Compare(mes1));
                        Assert.IsTrue(copy.Compare(mes2));
                        Assert.IsFalse(copy.Compare(mes3));
                        break;
                    case 3:
                        Assert.IsFalse(copy.Compare(mes1));
                        Assert.IsFalse(copy.Compare(mes2));
                        Assert.IsTrue(copy.Compare(mes3));
                        break;
                }
            }
        }

        [TestMethod]
        public void ReplyTest()
        {
            var now = DateTime.Now;
            Reply rep = new Reply(1, 1, 1, true, "Hello World", now.ToString());
            rep.SignMessage(_pri);
            Assert.AreEqual(rep.ServID,1);
            Assert.AreEqual(rep.SeqNr,1);
            Assert.AreEqual(rep.ViewNr,1);
            Assert.IsTrue(rep.Status);
            StringAssert.Contains(rep.Result,"Hello World");
            StringAssert.Contains(rep.Timestamp, now.ToString());
            _objectStore.Attach(rep);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage, false);
            Reply rep2 = _objectStore.Resolve<Reply>();
            Assert.IsTrue(rep.Compare(rep2));
        }

        [TestMethod]
        public void CheckpointTest()
        {
            var test = new Request(1,"Hello", "12:00");
            var testdig = Crypto.CreateDigest(test);
            var checkmes = new Checkpoint(1, 1,testdig);
            checkmes.SignMessage(_pri);
            Assert.AreEqual(checkmes.ServID,1);
            Assert.AreEqual(checkmes.StableSeqNr,1);
            Assert.IsTrue(checkmes.StateDigest.SequenceEqual(testdig));
            Assert.AreNotEqual(checkmes.Signature,null);
            _objectStore.Attach(checkmes);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage, false);
            Checkpoint copymes = _objectStore.Resolve<Checkpoint>();
            Assert.IsTrue(copymes.Compare(checkmes));
        }
    }
}