using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Certificates;

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
        public void ProtocolCertificateTest()
        {
            _objectStore = ObjectStore.New(_storage);
            Request req1 = new Request(1, "test1", DateTime.Now.ToString());
            Request req2 = new Request(2, "test2", DateTime.Now.ToString());
            ProtocolCertificate qcert1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Prepared);
            ProtocolCertificate qcert2 = new ProtocolCertificate(2, 2, Crypto.CreateDigest(req2), CertType.Committed);
            PhaseMessage pm1 = new PhaseMessage(1, 1, 1, Crypto.CreateDigest(req1), PMessageType.Commit);
            PhaseMessage pm2 = new PhaseMessage(2, 2, 2, Crypto.CreateDigest(req2), PMessageType.Prepare);
            req1.SignMessage(_prikey);
            req2.SignMessage(_prikey);
            pm1.SignMessage(_prikey);
            pm2.SignMessage(_prikey);
            qcert2.ProofList.Add(pm1);
            Assert.AreEqual(qcert1.SeqNr, 1);
            Assert.AreEqual(qcert1.ViewNr, 1);
            Assert.AreEqual(qcert1.ProofList.Count, 0);
            Assert.AreEqual(qcert1.CType, CertType.Prepared);
            Assert.AreEqual(qcert2.SeqNr, 2);
            Assert.AreEqual(qcert2.ViewNr, 2);
            Assert.AreEqual(qcert2.ProofList.Count, 1);
            Assert.AreEqual(qcert2.CType, CertType.Committed);

            _objectStore.Attach(qcert1);
            _objectStore.Attach(qcert2);
            _objectStore.Persist();
            _objectStore = null; //remove current state to test load functionality works
            _objectStore = ObjectStore.Load(_storage, false);
            var certificates = _objectStore.ResolveAll<ProtocolCertificate>();
            foreach (var copycert in certificates)
            {
                if (copycert.CType == CertType.Prepared)
                {
                    Assert.AreEqual(copycert.SeqNr, qcert1.SeqNr);
                    Assert.AreEqual(copycert.ViewNr, qcert1.ViewNr);
                    Assert.AreEqual(copycert.ProofList.Count, qcert1.ProofList.Count);
                    copycert.ProofList.Add(pm2);
                    Assert.AreNotEqual(copycert.ProofList.Count, qcert1.ProofList.Count);
                }
                else
                {
                    Assert.AreEqual(copycert.SeqNr, qcert2.SeqNr);
                    Assert.AreEqual(copycert.ViewNr, qcert2.ViewNr);
                    Assert.AreEqual(copycert.ProofList.Count, qcert2.ProofList.Count);
                    Assert.IsTrue(copycert.ProofList[0].Compare(qcert2.ProofList[0]));
                    copycert.ResetCertificate();
                    Assert.AreNotEqual(copycert.ProofList.Count, qcert2.ProofList.Count);
                }
            }
        }

        //Old test that doesn't work for some reason
        /*[TestMethod]
        public void CheckpointCertificateTest()
        {
            Request test = new Request(1, "test digest", "12:00");
            var testdigest = Crypto.CreateDigest(test);
            var testsource = new Source<CheckpointCertificate>();
            var cert = new CheckpointCertificate(2, testdigest, testsource);
            var listener = ListenforCheckpointMessage(testsource).GetAwaiter();
            var check1 = new Checkpoint(1, 2, testdigest);
            var check2 = new Checkpoint(2, 2, testdigest);
            check1.SignMessage(_prikey);
            check2.SignMessage(_prikey);
            cert.AppendProof(check1,_pubkey,1);
            cert.AppendProof(check2,_pubkey,1);
            Assert.AreEqual(cert.ProofList.Count, 2);
            _objectStore.Attach(cert);
            _objectStore.Persist();
            _objectStore = null;
            _objectStore = ObjectStore.Load(_storage);
            var copycert = _objectStore.Resolve<CheckpointCertificate>();
            Assert.AreEqual(copycert.ProofList.Count,2);
            var copycheck1 = copycert.ProofList[0];
            var copycheck2 = copycert.ProofList[1];
            Assert.IsTrue(copycheck1.Compare(check1));
            Assert.IsTrue(copycheck2.Compare(check2));
            Assert.AreNotEqual(check1.Signature,null);
            var check3 = new Checkpoint(0, 2, testdigest);
            check3.SignMessage(_prikey);
            copycert.AppendProof(check3,_pubkey,1);
            Assert.IsTrue(copycert.ValidateCertificate(1));
            Thread.Sleep(2000);

            Console.WriteLine("THIS SHIT IS BULLLLLLLLLLLL, THE RESULT WAS READY AGES AGO!!!!!!");
            var res = listener.GetResult();
            Assert.IsTrue(res.Stable);
        }*/
        
        [TestMethod]
        public void CheckpointCertificateTest()
        {
            var scheduler = ExecutionEngineFactory.StartNew(_storage);
           
            Request test = new Request(1, "test digest", "12:00");
            var testdigest = Crypto.CreateDigest(test);
            var testsource = new Source<CheckpointCertificate>();
            var check1 = new Checkpoint(1, 2, testdigest);
            var check2 = new Checkpoint(2, 2, testdigest);
            check1.SignMessage(_prikey);
            check2.SignMessage(_prikey);
            scheduler.Schedule(() =>
            {
                var cert = new CheckpointCertificate(2, testdigest, testsource);
                cert.AppendProof(check1,_pubkey,1);
                cert.AppendProof(check2,_pubkey,1);
                Assert.AreEqual(cert.ProofList.Count, 2);
                Roots.Entangle(cert);
                var listener = ListenforCheckpointMessage(testsource);
                Roots.Entangle(listener);
            });
            scheduler.Sync().Wait();
            scheduler.Dispose();

            scheduler = ExecutionEngineFactory.Continue(_storage);
            scheduler.Schedule(() =>
            {
                var copycert = Roots.Resolve<CheckpointCertificate>();
                var copylistener = Roots.Resolve<CTask<CheckpointCertificate>>();
                Assert.AreEqual(copycert.ProofList.Count, 2);
                var copycheck1 = copycert.ProofList[0];
                var copycheck2 = copycert.ProofList[1];
                Assert.IsTrue(copycheck1.Compare(check1));
                Assert.IsTrue(copycheck2.Compare(check2));
                Assert.AreNotEqual(check1.Signature, null);
                var check3 = new Checkpoint(0, 2, testdigest);
                check3.SignMessage(_prikey);
                copycert.AppendProof(check3, _pubkey, 1);
                Assert.IsTrue(copycert.ValidateCertificate(1));
                
                var res = copylistener.GetAwaiter().GetResult();
                Assert.IsTrue(res.Stable);
                Assert.AreEqual(res.ProofList.Count, 3);
                Assert.AreNotEqual(res.StateDigest,null);
                Assert.IsTrue(res.ProofList[0].Compare(check1));
            }).GetAwaiter().GetResult();
            Thread.Sleep(2000);
            scheduler.Sync().Wait();
        }
        
        public async CTask<CheckpointCertificate> ListenforCheckpointMessage(Source<CheckpointCertificate> checkbridge)
        {
            var rescert = await checkbridge.Next();
            Console.WriteLine("I got result!");
            return rescert;
        }
    }
}