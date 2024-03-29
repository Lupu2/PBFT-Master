using System;
using System.Security.Cryptography;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Certificates;
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
        
        [TestMethod]
        public void CheckpointCertificateTest()
        {
            var emits = new CAppendOnlyList<CheckpointCertificate>();
            
            var scheduler = ExecutionEngineFactory.StartNew(_storage);
            Request test = new Request(1, "test digest", "12:00");
            var testdigest = Crypto.CreateDigest(test);
            
            var testsource = new Source<CheckpointCertificate>();
            testsource.CallOnEvent(emits.Add);
            
            CDictionary<int, string> con = new CDictionary<int, string>();
            con[1] = "127.0.0.1:9001";
            var sh = new SourceHandler(null, null, null, null, null, null, null, null, testsource);
            var testserv = new Server(1, 1, 4, scheduler, 10, con[1], sh, con);
            var cert = new CheckpointCertificate(2, testdigest, testserv.EmitCheckpoint);
           //var listener = ListenforCheckpointMessage(testsource).GetAwaiter();
            var check1 = new Checkpoint(1, 2, testdigest);
            var check2 = new Checkpoint(2, 2, testdigest);
            check1.SignMessage(_prikey);
            check2.SignMessage(_prikey);
            cert.AppendProof(check1,_pubkey,1);
            cert.AppendProof(check2,_pubkey,1);
            Assert.AreEqual(cert.ProofList.Count, 2);
            scheduler.Entangle(cert).Wait();
            scheduler.Entangle(cert).Wait();
            scheduler.Entangle(testserv).Wait();
            scheduler.Entangle(testsource).Wait();
            scheduler.Entangle(emits).Wait();

            scheduler.Schedule(() =>
            {
                Roots.Resolve<Source<CheckpointCertificate>>().Emit(new CheckpointCertificate(0, new byte[]{}, null));
                Assert.IsTrue(Roots.Resolve<CAppendOnlyList<CheckpointCertificate>>().Count == 1);
            }).Wait();
            
            scheduler.Sync().Wait();
            var newscheduler = ExecutionEngineFactory.Continue(_storage);
            newscheduler.Schedule(() =>
            {
                var copyemits = Roots.Resolve<CAppendOnlyList<CheckpointCertificate>>();
                var copycert = Roots.Resolve<CheckpointCertificate>();
                var copyserv = Roots.Resolve<Server>();
                var copysource = Roots.Resolve<Source<CheckpointCertificate>>();

                copyserv.Subjects.CheckpointFinSubject = copysource;
                copyserv.CurSeqNr = 2;
                copycert.EmitCheckpoint = copyserv.EmitCheckpoint;
                
                Console.WriteLine("Callback: ");
                Console.WriteLine(copycert.EmitCheckpoint);
                //listener = _objectStore.Resolve<CAwaitable.Awaiter<>();
                Assert.AreEqual(copycert.ProofList.Count,2);
                var copycheck1 = copycert.ProofList[0];
                var copycheck2 = copycert.ProofList[1];
                Assert.IsTrue(copycheck1.Compare(check1));
                Assert.IsTrue(copycheck2.Compare(check2));
                Assert.AreNotEqual(check1.Signature,null);
                var check3 = new Checkpoint(0, 2, testdigest);
                check3.SignMessage(_prikey);
                copycert.AppendProof(check3, _pubkey,1);
                Assert.IsTrue(copycert.ValidateCertificate(1));
                Assert.IsTrue(copyemits.Count == 1);
                copysource.Emit(new CheckpointCertificate(0, new byte[]{}, null));
                Assert.IsTrue(copyemits.Count == 2);
            }).Wait();
        }

        //Old test that doesn't work for some reason
        /*[TestMethod]
        public void CheckpointCertificateSchedulerTest()
        {
            var scheduler = ExecutionEngineFactory.StartNew(_storage);
            Request test = new Request(1, "test digest", "12:00");
            var testdigest = Crypto.CreateDigest(test);
            var check1 = new Checkpoint(1, 2, testdigest);
            var check2 = new Checkpoint(2, 2, testdigest);
            scheduler.Schedule(() =>
            {
                var testsource = new Source<CheckpointCertificate>();
                var sh = new SourceHandler(null, null, null, null, null, testsource);
                CDictionary<int, string> con = new CDictionary<int, string>();
                con[1] = "127.0.0.1:9001";
                var serv = new Server(1, 1, 4, Engine.Current, 10, con[1], sh, con);
                Action<CheckpointCertificate> emit = serv.EmitCheckpoint;
                check1.SignMessage(_prikey);
                check2.SignMessage(_prikey);
                var cert = new CheckpointCertificate(2, testdigest, emit);
                cert.AppendProof(check1,_pubkey,1);
                cert.AppendProof(check2,_pubkey,1);
                Assert.AreEqual(cert.ProofList.Count, 2);
                Roots.Entangle(testsource);
                Roots.Entangle(serv);
                Roots.Entangle(cert);
            });
            scheduler.Sync().Wait();
            scheduler.Dispose();

            scheduler = ExecutionEngineFactory.Continue(_storage);
            scheduler.Schedule(() =>
            {
                var copysource = Roots.Resolve<Source<CheckpointCertificate>>();
                var copyserv = Roots.Resolve<Server>();
                copyserv.Subjects.CheckpointSubject = copysource;
                var copycert = Roots.Resolve<CheckpointCertificate>();
                copyserv.CurSeqNr = 2;
                copycert.EmitCheckpoint = copyserv.EmitCheckpoint;
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
                Assert.AreNotEqual(copyserv.StableCheckpointsCertificate, null);
                Assert.AreEqual(copyserv.StableCheckpointsCertificate.LastSeqNr, copycert.LastSeqNr);
                Assert.IsTrue(copyserv.StableCheckpointsCertificate.StateDigest.SequenceEqual(copycert.StateDigest));
                //var res = copysource.Next().GetAwaiter().GetResult();
                //Assert.IsTrue(res.Stable);
                //Assert.AreEqual(res.ProofList.Count, 3);
                //Assert.AreNotEqual(res.StateDigest,null);
                //Assert.IsTrue(res.ProofList[0].Compare(check1));
            }).GetAwaiter().GetResult();
            Thread.Sleep(2000);
            scheduler.Sync().Wait();
        }*/
        
        public async CTask<CheckpointCertificate> ListenforCheckpointMessage(Source<CheckpointCertificate> checkbridge)
        {
            var rescert = await checkbridge.Next();
            Console.WriteLine("I got result!");
            return rescert;
        }

        [TestMethod]
        public void ViewChangeCertificateTest()
        {
            
        }
    }
}