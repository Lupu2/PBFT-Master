using System;
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
    public class ViewPrimaryTests
    {
        [TestMethod]
        public void ViewPrimaryTest()
        {
            ViewPrimary vp = new ViewPrimary(4);
            Assert.AreEqual(vp.ViewNr, 0);
            Assert.AreEqual(vp.ServID, 0);
            vp.NextPrimary();
            Assert.AreEqual(vp.ViewNr, 1);
            Assert.AreEqual(vp.ServID, 1);
            vp.UpdateView(3);
            Assert.AreEqual(vp.ViewNr, 3);
            Console.WriteLine(vp.ServID);
            Assert.AreEqual(vp.ServID, 3);
            vp.NextPrimary();
            Assert.AreEqual(vp.ViewNr, 4);
            Assert.AreEqual(vp.ServID, 0);
        }

        [TestMethod]
        public void ServerPrimaryTest()
        {
            var sh = new SourceHandler(
                new Source<Request>(),
                new Source<PhaseMessage>(),
                new Source<bool>(),
                new Source<ViewChangeCertificate>(),
                new Source<NewView>(), new
                    Source<CheckpointCertificate>()
            );
            Server testserv = new Server(0, 0, 4, null, 50, "127.0.0.1:9000", sh, new CDictionary<int, string>());
            Assert.AreEqual(testserv.CurPrimary.ServID, 0);
            Assert.AreEqual(testserv.CurView, 0);
            Assert.IsTrue(testserv.IsPrimary());
            testserv.CurView = 1;
            Assert.IsFalse(testserv.IsPrimary());
            Assert.AreEqual(testserv.CurView, 1);
        }

        [TestMethod]
        public void MakePrepareMessageTest()
        {
            int lowbound = 0;
            int highbound = 5;
            var server = new Server(1, 0, 4, null, 5, "127.0.0.1:9001", null, new CDictionary<int, string>());
            server.CurPrimary.NextPrimary();
            server.CurView++;
            CDictionary<int, ProtocolCertificate> protocerts = new CDictionary<int, ProtocolCertificate>();
            var dig1 = Crypto.CreateDigest(new Request(1, "Hello", "12:00"));
            var dig2 = Crypto.CreateDigest(new Request(2, "Dumbo", "12:01"));
            var dig3 = Crypto.CreateDigest(new Request(3, "latin", "12:02"));
            var dig6 = Crypto.CreateDigest(new Request(1, "something", "12:05"));
            var proto1 = new ProtocolCertificate(0, 1, dig1, CertType.Prepared);
            var proto2 = new ProtocolCertificate(1, 1, dig2, CertType.Prepared);
            var proto3 = new ProtocolCertificate(2, 1, dig3, CertType.Prepared);
            var proto6 = new ProtocolCertificate(5, 1, dig6, CertType.Prepared);
            protocerts[0] = proto1;
            protocerts[1] = proto2;
            protocerts[2] = proto3;
            protocerts[5] = proto6;
            
            var prepreplist = server.CurPrimary.MakePrepareMessages(protocerts, lowbound, highbound);
            Assert.AreEqual(prepreplist.Count, 6);
            Assert.IsTrue(prepreplist[0].Digest.SequenceEqual(dig1));
            Assert.IsTrue(prepreplist[1].Digest.SequenceEqual(dig2));
            Assert.IsTrue(prepreplist[2].Digest.SequenceEqual(dig3));
            Assert.AreEqual(prepreplist[3].Digest, null);
            Assert.AreEqual(prepreplist[4].Digest, null);
            Assert.IsTrue(prepreplist[5].Digest.SequenceEqual(dig6));
            Assert.IsTrue(prepreplist[0].SeqNr == 0);
            Assert.IsTrue(prepreplist[1].SeqNr == 1);
            Assert.IsTrue(prepreplist[2].SeqNr == 2);
            Assert.IsTrue(prepreplist[3].SeqNr == 3);
            Assert.IsTrue(prepreplist[4].SeqNr == 4);
            Assert.IsTrue(prepreplist[5].SeqNr == 5);
            foreach (var pm in prepreplist)
            {
                Assert.AreEqual(pm.ServID, server.ServID);
                Assert.AreEqual(pm.ViewNr, server.CurView);
                Assert.AreEqual(pm.PhaseType, PMessageType.PrePrepare);
                Assert.AreEqual(pm.Signature, null);
            }
        }
    }
}