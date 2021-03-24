using System;
using System.Linq;
using System.Security.Cryptography;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Persistency
{
    [TestClass]
    public class ServerPersistencyTests
    {
        private SimpleFileStorageEngine _storage;
        private ObjectStore _objectStore;
        private RSAParameters _prikey;
        private RSAParameters _pubkey;
        
        [TestInitialize]
        public void StorageIntialization()
        {
            _storage = new SimpleFileStorageEngine("testserver.txt", true);
            _objectStore = ObjectStore.New(_storage); 
            (_prikey, _pubkey) = Crypto.InitializeKeyPairs();
        }
        
        [TestMethod]
        public void ServerPersistencyInfoTest()
        {   //Remember to update this test each time the server object is updated.
            var serv = new Server(0, 0, 4, null, 20, "127.0.0.1:9000", new Source<Request>(),
                new Source<PhaseMessage>(), new Source<ViewChange>(), new Source<ViewChangeCertificate>(),new CDictionary<int, string>());
            serv.ServerContactList[0] = "127.0.0.1:9000";
            var rep = new Reply(1, 1, 1, false, "error", DateTime.Now.ToString());
            rep.SignMessage(_prikey);
            serv.ReplyLog.Set(1, rep);
            serv.ClientActive.Set(1,true);
            _objectStore.Attach(serv);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage);
            var servcopy = _objectStore.Resolve<Server>();
            Assert.AreEqual(servcopy.ServID, serv.ServID);
            Assert.AreEqual(servcopy.CurView, serv.CurView);
            Assert.AreEqual(servcopy.CurSeqNr, serv.CurSeqNr);
            Assert.AreEqual(servcopy.CurSeqRange.Start.Value, servcopy.CurSeqRange.Start.Value);
            Assert.AreEqual(servcopy.CurSeqRange.End.Value, servcopy.CurSeqRange.End.Value);
            Assert.AreEqual(servcopy.CurPrimary.ViewNr, serv.CurPrimary.ViewNr);
            Assert.AreEqual(servcopy.CurPrimary.ServID, serv.CurPrimary.ServID);
            Assert.AreEqual(servcopy.TotalReplicas, serv.TotalReplicas);
            Assert.AreEqual(servcopy.CheckpointConstant,serv.CheckpointConstant);
            Assert.AreEqual(servcopy.StableCheckpoints, null);
            Assert.IsTrue(servcopy.ReplyLog[1].Compare(serv.ReplyLog[1]));
            Assert.IsTrue(servcopy.ClientActive[1]);
        }

        [TestMethod]
        public void ViewPrimaryTest()
        {
            ViewPrimary vm = new ViewPrimary(4);
            Assert.AreEqual(vm.ServID,0);
            Assert.AreEqual(vm.ViewNr,0);
            vm.NextPrimary();
            
            _objectStore.Attach(vm);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage, false);
            ViewPrimary vmcopy = _objectStore.Resolve<ViewPrimary>();
            Assert.AreEqual(vmcopy.ServID,1);
            Assert.AreEqual(vmcopy.ServID,1);
            Assert.AreEqual(vm.ServID, vmcopy.ServID);
            Assert.AreEqual(vm.ViewNr, vmcopy.ViewNr);
        }
    }
}