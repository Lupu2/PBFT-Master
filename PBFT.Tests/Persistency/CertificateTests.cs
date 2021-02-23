using System;
using System.Linq;
using System.Security.Cryptography;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Persistency
{
    [TestClass]
    public class CertificateTests
    {
        private InMemoryStorageEngine _storage;
        private ObjectStore _objectStore;
        private RSAParameters _prikey;
        private RSAParameters _pubkey;
        
        [TestInitialize]
        public void StorageIntialization()
        {
            _storage = new InMemoryStorageEngine();
            _objectStore = ObjectStore.New(_storage);
            (_prikey, _pubkey) = Crypto.InitializeKeyPairs();
        }
        
        [TestMethod]
        public void QCertificateTest()
        {
            QCertificate qcert1 = new QCertificate(1, 1, CertType.Prepared);
            QCertificate qcert2 = new QCertificate(2, 2, CertType.Committed);
            Request req1 = new Request(1, "test1", DateTime.Now.ToString());
            Request req2 = new Request(2, "test2", DateTime.Now.ToString());
            PhaseMessage pm1 = new PhaseMessage(1, 1, 1, Crypto.CreateDigest(req1), PMessageType.Commit);
            PhaseMessage pm2 = new PhaseMessage(2, 2, 2, Crypto.CreateDigest(req2), PMessageType.Prepare);
            req1.SignMessage(_prikey);
            req2.SignMessage(_prikey);
            pm1.SignMessage(_prikey);
            pm2.SignMessage(_prikey);
            qcert2.ProofList.Add(pm1);
            Assert.AreEqual(qcert1.SeqNr,1);
            Assert.AreEqual(qcert1.ViewNr,1);
            Assert.AreEqual(qcert1.ProofList.Count,0);
            Assert.AreEqual(qcert1.CType,CertType.Prepared);
            Assert.AreEqual(qcert2.SeqNr,2);
            Assert.AreEqual(qcert2.ViewNr,2);
            Assert.AreEqual(qcert2.ProofList.Count,1);
            Assert.AreEqual(qcert2.CType,CertType.Committed);
            
            _objectStore.Attach(qcert1);
            _objectStore.Attach(qcert2);
            _objectStore.Persist();
            _objectStore = ObjectStore.Load(_storage, false);
            var certificates = _objectStore.ResolveAll<QCertificate>();
            foreach (var copycert in certificates)
            {
                if (copycert.CType == CertType.Prepared)
                {
                    Assert.AreEqual(copycert.SeqNr,qcert1.SeqNr);
                    Assert.AreEqual(copycert.ViewNr,qcert1.ViewNr);
                    Assert.AreEqual(copycert.ProofList.Count, qcert1.ProofList.Count);
                    copycert.ProofList.Add(pm2);
                    Assert.AreNotEqual(copycert.ProofList.Count, qcert1.ProofList.Count);
                }
                else
                {
                    Assert.AreEqual(copycert.SeqNr,qcert2.SeqNr);
                    Assert.AreEqual(copycert.ViewNr,qcert2.ViewNr);
                    Assert.AreEqual(copycert.ProofList.Count, qcert2.ProofList.Count);
                    Assert.IsTrue(copycert.ProofList[0].Compare(qcert2.ProofList[0]));
                    copycert.ResetCertificate();
                    Assert.AreNotEqual(copycert.ProofList.Count, qcert2.ProofList.Count);
                }
            }
        }
    }
}