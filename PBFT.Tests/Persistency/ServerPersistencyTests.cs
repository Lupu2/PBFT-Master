using Cleipnir.ExecutionEngine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Persistency
{
    [TestClass]
    public class ServerPersistencyTests
    {
        public void ServerPersistencyInfoTest()
        {
            var storageEngine = new SimpleFileStorageEngine(".PBFTStorage.txt", true); //change to false when done debugging
            var scheduler = ExecutionEngineFactory.StartNew(storageEngine);
            var serv = new Server(0,0,4,scheduler,20,"127.0.0.1:9000", new Source<Request>(), new Source<PhaseMessage>());
            
            Assert.AreEqual(serv.ServID,0);
            Assert.AreEqual(serv.CurView,0);
            Assert.AreEqual(serv.TotalReplicas,4);
            
            scheduler.Dispose();
            storageEngine.Dispose();

            var storageEngine2 = new SimpleFileStorageEngine(".PBFTStorage.txt", false);
            scheduler = ExecutionEngineFactory.Continue(storageEngine2);
            
        }
    }
}