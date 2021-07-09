using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;
using PBFT.Replica.Protocol;
namespace PBFT.Tests.Replica.Protocol
{
    [TestClass]
    public class ViewChangeListenerTests
    {
        private Engine _scheduler;
        private bool _viewlisten = false;
        private bool _shutdownlisten = false;
        [TestInitialize]
        public void SchedulerInitializer()
        {
            var storage = new InMemoryStorageEngine();
            _scheduler = ExecutionEngineFactory.StartNew(storage);
        }

        [TestMethod]
        public void ViewChangeNoPreviousInfoListenerTest()
        {
            var (pri0, pub0) = Crypto.InitializeKeyPairs();
            var (pri1, pub1) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            var (pri3, pub3) = Crypto.InitializeKeyPairs();
            var keys = new Dictionary<int, RSAParameters>();
            keys[0] = pub0;
            keys[1] = pub1;
            keys[2] = pub2;
            keys[3] = pub3;
            Source<ViewChange> viewbridge = new Source<ViewChange>();
            ViewPrimary vp = new ViewPrimary(1, 1, 4);
            var viewlistener = new ViewChangeListener(1, Quorum.CalculateFailureLimit(4), vp, viewbridge, false);
            var viewcert = new ViewChangeCertificate(vp, null, null, null);
            var view0 = new ViewChange(-1,0,1,null, new CDictionary<int, ProtocolCertificate>());
            view0.SignMessage(pri0);
            var view1 = new ViewChange(-1,1,1,null, new CDictionary<int, ProtocolCertificate>());
            view1.SignMessage(pri1);
            var view2 = new ViewChange(-1,2,1,null, new CDictionary<int, ProtocolCertificate>());
            view2.SignMessage(pri2);
            var view3 = new ViewChange(-1,3,1,null, new CDictionary<int, ProtocolCertificate>());
            view3.SignMessage(pri3);
            viewlistener.Listen(viewcert, keys, ListenForEmit, null);
            
            _scheduler.Schedule(() =>
            {
                viewbridge.Emit(view0);
                Thread.Sleep(500);
                viewbridge.Emit(view1);
                Thread.Sleep(500);
                viewbridge.Emit(view2);
            });
            Thread.Sleep(3000);
            Assert.AreEqual(viewcert.ProofList.Count, 3);
            Assert.IsTrue(viewcert.IsValid());
            Assert.IsTrue(_viewlisten);
            _viewlisten = false;
        }
        
        [TestMethod]
        public void ViewChangeWithPreviousInfoListenerTest()
        {
            var (pri0, pub0) = Crypto.InitializeKeyPairs();
            var (pri1, pub1) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            var (pri3, pub3) = Crypto.InitializeKeyPairs();
            var keys = new Dictionary<int, RSAParameters>();
            keys[0] = pub0;
            keys[1] = pub1;
            keys[2] = pub2;
            keys[3] = pub3;
            var dig = Crypto.CreateDigest(new Request(1, "12:00"));
            var dig2 = Crypto.CreateDigest(new Request(2, "12:00"));
            var checkcert = new CheckpointCertificate(5, dig, null);
            checkcert.Stable = true;
            var protocerts = new CDictionary<int, ProtocolCertificate>();
            var protocert6 = new ProtocolCertificate(6, 0, dig2, CertType.Prepared);
            var protocert7 = new ProtocolCertificate(7, 0, dig2, CertType.Prepared);
            var protocert8 = new ProtocolCertificate(8, 0, dig2, CertType.Prepared);
            protocerts[6] = protocert6;
            protocerts[7] = protocert7;
            protocerts[8] = protocert8;
            
            Source<ViewChange> viewbridge = new Source<ViewChange>();
            ViewPrimary vp = new ViewPrimary(1, 1, 4);
            var viewlistener = new ViewChangeListener(1, Quorum.CalculateFailureLimit(4), vp, viewbridge, false);
            var viewcert = new ViewChangeCertificate(vp, checkcert, null, null);
            var view0 = new ViewChange(5,0,1, checkcert, protocerts);
            view0.SignMessage(pri0);
            var view1 = new ViewChange(5,1,1, checkcert, protocerts);
            view1.SignMessage(pri1);
            var view2 = new ViewChange(5,2,1, checkcert, protocerts);
            view2.SignMessage(pri2);
            var view3 = new ViewChange(5,3,1, checkcert, protocerts);
            view3.SignMessage(pri3);
            viewlistener.Listen(viewcert, keys, ListenForEmit, null);
            
            _scheduler.Schedule(() =>
            {
                viewbridge.Emit(view0);
                Thread.Sleep(500);
                viewbridge.Emit(view1);
                Thread.Sleep(500);
                viewbridge.Emit(view2);
            });
            Thread.Sleep(3000);
            Assert.AreEqual(viewcert.ProofList.Count, 3);
            Assert.IsTrue(viewcert.IsValid());
            Assert.IsTrue(_viewlisten);
            _viewlisten = false;
            
            ViewPrimary vp2 = new ViewPrimary(2, 2, 4);
            var viewcert2 = new ViewChangeCertificate(vp2, checkcert, null, null);
            
            var view02 = new ViewChange(5,0,2, checkcert, protocerts);
            view02.SignMessage(pri0);
            var view12 = new ViewChange(5,1,2, checkcert, protocerts);
            view12.SignMessage(pri1);
            var view22 = new ViewChange(5,2,2, checkcert, protocerts);
            view22.SignMessage(pri2);
            var view32 = new ViewChange(5,3,2, checkcert, protocerts);
            view32.SignMessage(pri3);
            var viewlistener2 = new ViewChangeListener(2, Quorum.CalculateFailureLimit(4), vp2, viewbridge, false);
            viewlistener2.Listen(viewcert2, keys, ListenForEmit, null);
            
            _scheduler.Schedule(() =>
            {
                viewbridge.Emit(view02);
                Thread.Sleep(500);
                viewbridge.Emit(view12);
                Thread.Sleep(500);
                viewbridge.Emit(view22);
            });
            Thread.Sleep(3000);
            Assert.AreEqual(viewcert2.ProofList.Count, 3);
            Assert.IsTrue(viewcert2.IsValid());
            Assert.IsTrue(_viewlisten);
            _viewlisten = false;
        }
        
        [TestMethod]
        public void ShutdownTest()
        {
            Source<ViewChange> viewbridge = new Source<ViewChange>();

            var (pri0, pub0) = Crypto.InitializeKeyPairs();
            var (pri1, pub1) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            var (pri3, pub3) = Crypto.InitializeKeyPairs();
            var keys = new Dictionary<int, RSAParameters>();
            keys[0] = pub0;
            keys[1] = pub1;
            keys[2] = pub2;
            keys[3] = pub3;
            var dig = Crypto.CreateDigest(new Request(1, "12:00"));
            var dig2 = Crypto.CreateDigest(new Request(2, "12:00"));
            var checkcert = new CheckpointCertificate(5, dig, null);
            checkcert.Stable = true;
            var protocerts = new CDictionary<int, ProtocolCertificate>();
            var protocert6 = new ProtocolCertificate(6, 0, dig2, CertType.Prepared);
            var protocert7 = new ProtocolCertificate(7, 0, dig2, CertType.Prepared);
            var protocert8 = new ProtocolCertificate(8, 0, dig2, CertType.Prepared);
            protocerts[6] = protocert6;
            protocerts[7] = protocert7;
            protocerts[8] = protocert8;
            
            ViewPrimary vp = new ViewPrimary(1, 1, 4);
            var viewlistener = new ViewChangeListener(1, Quorum.CalculateFailureLimit(4), vp, viewbridge, true);
            var viewcert = new ViewChangeCertificate(vp, checkcert, null, null);
            var view0 = new ViewChange(5,0,1, checkcert, protocerts);
            view0.SignMessage(pri0);
            var view1 = new ViewChange(5,1,1, checkcert, protocerts);
            view1.SignMessage(pri1);
            var view2 = new ViewChange(5,2,1, checkcert, protocerts);
            view2.SignMessage(pri2);
            var view3 = new ViewChange(5,3,1, checkcert, protocerts);
            view3.SignMessage(pri3);
            viewlistener.Listen(viewcert, keys, ListenForEmit, ListenForShutdown);
            Thread.Sleep(500);
            _scheduler.Schedule(() =>
            {
                viewbridge.Emit(view0);
                Thread.Sleep(500);
                viewbridge.Emit(view1);
                Thread.Sleep(500);
            });
            Thread.Sleep(3000);
            Assert.IsTrue(_shutdownlisten);
            _scheduler.Schedule(() => viewbridge.Emit(view2));
            Thread.Sleep(1000);
            Assert.IsTrue(viewcert.IsValid());
            Assert.IsTrue(_viewlisten);
            _viewlisten = false;
            _shutdownlisten = false;
        }

        public void ListenForEmit()
        {
            Console.WriteLine("ListenForEmit");
            _viewlisten = true;
            Console.WriteLine("ListenForEmit is finished");
        }
        
        public void ListenForShutdown()
        {
            Console.WriteLine("ListenForShutdown");
            _shutdownlisten = true;
            Console.WriteLine("ListenForShutdown is finished");
        }
    }
}