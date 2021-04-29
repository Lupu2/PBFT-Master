using System;
using System.Threading;
using System.Threading.Channels;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Tests.Replica.Protocol
{
    [TestClass]
    public class ReplyCertificateTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            var orgreq = new Request(1,"Hello", "12:00");
            var repcert = new ReplyCertificate(orgreq);
            Assert.IsTrue(orgreq.Compare(repcert.RequestOrg));
            Assert.AreEqual(repcert.ProofList.Count, 0);
            Assert.IsFalse(repcert.IsValid());
        }

        [TestMethod]
        public void QReachedTests()
        {
            var orgreq = new Request(1, "Hello", "12:00");
            var repcert = new ReplyCertificate(orgreq,true);
            Assert.AreEqual(repcert.ProofList.Count, 0);
            var rep1 = new Reply(1, 1,1, 1, true, orgreq.Message, orgreq.Timestamp);
            repcert.ProofList.Add(rep1);
            Assert.IsFalse(repcert.WeakQReached(1));
            repcert.ProofList.Add(rep1);
            Assert.IsFalse(repcert.WeakQReached(1));
            var rep2 = new Reply(2, 1, 2, 1, true, orgreq.Message, orgreq.Timestamp);
            repcert.ProofList.Add(rep2);
            Assert.IsTrue(repcert.WeakQReached(1));
            Assert.IsFalse(repcert.WeakQReached(2));
            Assert.IsFalse(repcert.QReached(1));
            var rep3 = new Reply(3, 1, 3, 1, true, orgreq.Message, orgreq.Timestamp);
            repcert.ProofList.Add(rep3);
            Assert.IsTrue(repcert.QReached(1));
            Assert.IsTrue(repcert.WeakQReached(2));
            Assert.IsFalse(repcert.QReached(2));
        }

        [TestMethod]
        public void ProofsValidTests()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var orgreq = new Request(1, "Hello", "12:00");
            var repcert = new ReplyCertificate(orgreq);
            Assert.IsFalse(repcert.ProofsAreValid());
            var rep1 = new Reply(1, 1,1, 1, true, orgreq.Message, orgreq.Timestamp);
            rep1.SignMessage(pri);
            repcert.ProofList.Add(rep1);
            Assert.IsTrue(repcert.ProofsAreValid());
            var rep2 = new Reply(2, 1,1, 1, true, orgreq.Message, orgreq.Timestamp);
            repcert.ProofList.Add(rep2);
            Assert.IsFalse(repcert.ProofsAreValid()); //testing signature
            rep2.SignMessage(pri);
            Assert.IsTrue(repcert.ProofsAreValid());
            var rep3 = new Reply(3, 1,1, 1, false, "", orgreq.Timestamp);
            rep3.SignMessage(pri);
            repcert.ProofList.Add(rep3);
            Assert.IsFalse(repcert.ProofsAreValid());
            Assert.AreEqual(repcert.ProofList.Count, 3);
            repcert.ResetCertificate();
            Assert.AreEqual(repcert.ProofList.Count, 0);
            var rep1f = new Reply(1, 1, 2, 1, false, "Failure", orgreq.Timestamp);
            var rep2f = new Reply(2, 1, 2, 1, false, "Failure", orgreq.Timestamp);
            var rep3f = new Reply(3, 1, 2, 1, false, "Failure", orgreq.Timestamp);
            rep1f.SignMessage(pri);
            rep2f.SignMessage(pri);
            rep3f.SignMessage(pri);
            repcert.ProofList.Add(rep1f);
            repcert.ProofList.Add(rep2f);
            repcert.ProofList.Add(rep3f);
            Assert.IsTrue(repcert.ProofsAreValid());
        }
        
        [TestMethod]
        public void ValidateCertificateTest()
        {
            var (pri, _) = Crypto.InitializeKeyPairs();
            
            Request req1 = new Request(1, "Hello World", DateTime.Now.ToString());
            Thread.Sleep(1000);
            Request req2 = new Request(1, "Bye Cruel World!", DateTime.Now.ToString());
            req1.SignMessage(pri);
            //byte[] dig = req1.SerializeToBuffer();
            var repcert1 = new ReplyCertificate(req1);
            var repcert2 = new ReplyCertificate(req2);
            var rep1 = new Reply(1, 1, 1, 1, true, req1.Message, req1.Timestamp);
            var rep2 = new Reply(2, 1, 1, 1, true, req1.Message, req1.Timestamp);
            var rep3 = new Reply(3, 1, 1, 1, true, req1.Message, req1.Timestamp);
            var rep4 = new Reply(1, 1, 1, 1, true, req2.Message, req2.Timestamp);
            var rep5 = new Reply(2, 1, 1, 1, true, req2.Message, req2.Timestamp);
            var rep6 = new Reply(3, 1, 1, 1, true, req2.Message, req2.Timestamp);
            var rep1f = new Reply(1, 1, 1, 1, false, "Failure", req1.Timestamp);
            var rep2f = new Reply(1, 1,1, 1, false, "Failure", req1.Timestamp);
            var rep3f = new Reply(1, 1, 1, 1, false, "Failure", req1.Timestamp);
            //Signature check
            repcert1.ProofList.Add(rep1);
            repcert1.ProofList.Add(rep2);
            repcert1.ProofList.Add(rep3);
            Assert.IsFalse(repcert1.ValidateCertificate(1));
            repcert1.ResetCertificate();
            rep1.SignMessage(pri);
            rep2.SignMessage(pri);
            rep3.SignMessage(pri);
            rep4.SignMessage(pri);
            rep5.SignMessage(pri);
            rep6.SignMessage(pri);
            rep1f.SignMessage(pri);
            rep2f.SignMessage(pri);
            rep3f.SignMessage(pri);
            
            repcert1.ProofList.Add(rep1);
            repcert1.ProofList.Add(rep2);
            repcert1.ProofList.Add(rep3);
            Assert.IsTrue(repcert1.ValidateCertificate(1));
            repcert1.ResetCertificate();
            repcert1.ProofList.Add(rep1);
            repcert1.ProofList.Add(rep2);
            repcert1.ProofList.Add(rep3);
            Assert.IsFalse(repcert1.ValidateCertificate(2));
            
            //factoring duplicates
            repcert2.ProofList.Add(rep4);
            repcert2.ProofList.Add(rep4);
            repcert2.ProofList.Add(rep5);
            Assert.IsFalse(repcert2.ValidateCertificate(1));
            repcert2.ProofList.Add(rep6);
            Assert.IsTrue(repcert2.ValidateCertificate(1));
            repcert1.ProofList.Add(rep1f);
            Assert.IsFalse(repcert1.ValidateCertificate(1));
            repcert1.ResetCertificate();
            repcert2.ResetCertificate();
            Assert.AreEqual(repcert1.ProofList.Count, 0);
            Assert.AreEqual(repcert2.ProofList.Count, 0);
            
            repcert1.ProofList.Add(rep1f);
            repcert1.ProofList.Add(rep2f);
            repcert1.ProofList.Add(rep3f);
            Assert.IsTrue(repcert1.ValidateCertificate(1));
            repcert2.ValStrength = true;
            repcert2.ProofList.Add(rep4);
            repcert2.ProofList.Add(rep5);
            Assert.IsTrue(repcert2.ValidateCertificate(1));
            repcert1.ResetCertificate();
            repcert2.ResetCertificate();
            Assert.AreEqual(repcert1.ProofList.Count, 0);
            Assert.AreEqual(repcert2.ProofList.Count, 0);
            
            //testing mixed
            repcert1.ProofList.Add(rep1);
            repcert1.ProofList.Add(rep2);
            repcert1.ProofList.Add(rep4);
            Assert.IsFalse(repcert1.ValidateCertificate(1));
            repcert1.ResetCertificate();
            repcert1.ProofList.Add(rep1);
            repcert1.ProofList.Add(rep2);
            repcert1.ProofList.Add(rep3f);
            Assert.IsFalse(repcert1.ValidateCertificate(1));
        }
    }
}