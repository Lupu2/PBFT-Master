using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Tests.Helper
{
    [TestClass]
    public class CryptoTests
    {
        [TestMethod]
        public void InitializeKeyPairsTest()
        {
            //Keep private key secret, can't leak info about: p,q & d, put has the value for everything in the object
            //PubKey contains only info for Exponent e & Modulus n, can be freely distributed to other machines.
            var (pri, pub) = Crypto.InitializeKeyPairs();
            //PubKey tests
            Assert.IsNull(pub.D);
            Assert.IsNull(pub.P);
            Assert.IsNull(pub.Q);
            Assert.IsNull(pub.DP);
            Assert.IsNull(pub.InverseQ);
            Assert.IsNotNull(pub.Exponent);
            Assert.IsNotNull(pub.Modulus);
            
            
            //Private key tests
            Assert.IsNotNull(pri.D);
            Assert.IsNotNull(pri.P);
            Assert.IsNotNull(pri.Q);
            Assert.IsNotNull(pri.Exponent);
            Assert.IsNotNull(pri.Modulus);
            Assert.IsNotNull(pri.DP);
            Assert.IsNotNull(pri.InverseQ);
        }
        
        [TestMethod]
        public void MessageDigestTest()
        {
            Request req1 = new Request(1, "Hello World", DateTime.Now.ToString());
            Request req2 = new Request(2, "Hello World", DateTime.Now.ToString());
            byte[] testhash;
            using (var sha = SHA256.Create())
            {
                testhash = sha.ComputeHash(req1.SerializeToBuffer());
            }

            byte[] dig1 = Crypto.CreateDigest(req1);
            byte[] dig2 = Crypto.CreateDigest(req2);
            Assert.AreNotEqual(BitConverter.ToString(dig1),BitConverter.ToString(dig2));
            //Assert.IsTrue(dig1.Equals(testhash));
            Assert.AreEqual(BitConverter.ToString(dig1),BitConverter.ToString(testhash));
        }
        
        [TestMethod]
        public void VerifySignatureTest()
        {
            var request = new Request(1, "Hello World", DateTime.Now.ToString());
            byte[] dig = Crypto.CreateDigest(request);
            var reply = new Reply(1, 1, 1, 1, true, "Hello World", DateTime.Now.ToString());
            var pm = new PhaseMessage(1, 1, 1, dig, PMessageType.PrePrepare);
            var pm2 = new PhaseMessage(2, 2, 2, dig, PMessageType.Prepare);
            var (pri,pub) = Crypto.InitializeKeyPairs();
            var (pri2, pub2) = Crypto.InitializeKeyPairs();
            
            request.SignMessage(pri);
            reply.SignMessage(pri2);
            pm.SignMessage(pri);
            pm2.SignMessage(pri2);
            Assert.IsTrue(Crypto.VerifySignature(
                request.Signature,
                request.CreateCopyTemplate().SerializeToBuffer(),
                pub)
            );
            Assert.IsFalse(Crypto.VerifySignature(
                request.Signature,
                request.CreateCopyTemplate().SerializeToBuffer(),
                pub2));
            Assert.IsFalse(Crypto.VerifySignature(
                request.Signature,
                request.SerializeToBuffer(),
                pub)
            );
            Assert.IsTrue(Crypto.VerifySignature(
                reply.Signature,
                reply.CreateCopyTemplate().SerializeToBuffer(),
                pub2)
            );
            Assert.IsFalse(Crypto.VerifySignature(
                reply.Signature, 
                reply.CreateCopyTemplate().SerializeToBuffer(), 
                pub)
            );
            Assert.IsFalse(Crypto.VerifySignature(
                reply.Signature, 
                dig,
                pub2)
            );
            Assert.IsTrue(Crypto.VerifySignature(
                pm.Signature, 
                pm.CreateCopyTemplate().SerializeToBuffer(),
                pub)
            );
            Assert.IsFalse(Crypto.VerifySignature(
                pm.Signature,
                pm.CreateCopyTemplate().SerializeToBuffer(),
                pub2
                    )
            );
            Assert.IsTrue(Crypto.VerifySignature(
                pm2.Signature,
                pm2.CreateCopyTemplate().SerializeToBuffer(),
                pub2
                )
            );
            Assert.IsFalse(Crypto.VerifySignature(
                pm2.Signature,
                pm2.CreateCopyTemplate().SerializeToBuffer(),
                pub)
            );
            Assert.IsTrue(Crypto.VerifySignature(
                request.Signature,
                request.CreateCopyTemplate().SerializeToBuffer(),
                pri)
            );
            Assert.IsTrue(Crypto.VerifySignature(
                reply.Signature,
                reply.CreateCopyTemplate().SerializeToBuffer(),
                pri2)
            );
        }
    }
}