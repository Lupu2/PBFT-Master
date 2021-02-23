using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Replica
{
    [TestClass]
    public class ViewPrimaryTests
    {
        [TestMethod]
        public void ViewPrimaryTest()
        {
            ViewPrimary vp = new ViewPrimary(4);
            Assert.AreEqual(vp.ViewNr,0);
            Assert.AreEqual(vp.ServID, 0);
            vp.NextPrimary();
            Assert.AreEqual(vp.ViewNr, 1);
            Assert.AreEqual(vp.ServID,1);
            vp.UpdateView(3);
            Assert.AreEqual(vp.ViewNr,3);
            Console.WriteLine(vp.ServID);
            Assert.AreEqual(vp.ServID,3);
            vp.NextPrimary();
            Assert.AreEqual(vp.ViewNr,4);
            Assert.AreEqual(vp.ServID,0);
        }

        [TestMethod]
        public void ServerPrimaryTest()
        {
            //TODO update when new view functionality has been added to server
            Server testserv = new Server(0,0,4,null,50,"127.0.0.1:9000", new Source<Request>(),new Source<PhaseMessage>(), new CDictionary<int, string>());
            Assert.AreEqual(testserv.CurPrimary.ServID,0);
            Assert.AreEqual(testserv.CurView,0);
            Assert.IsTrue(testserv.IsPrimary());
            testserv.CurView = 1;
            Assert.IsFalse(testserv.IsPrimary());
            Assert.AreEqual(testserv.CurView,1);
        }
    }
}