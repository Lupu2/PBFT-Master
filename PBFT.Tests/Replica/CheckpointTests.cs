using System;
using System.Linq;
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

namespace PBFT.Tests.Replica
{
    [TestClass]
    public class CheckpointTests
    {
        private Engine _scheduler;
        [TestInitialize]
        public void SchedulerInitializer()
        {
            var storage = new InMemoryStorageEngine();
            _scheduler = ExecutionEngineFactory.StartNew(storage);
        }
        
        [TestMethod]
        public void ProofsAreValidWithDigestTest()
        {
            var (_pri, _) = Crypto.InitializeKeyPairs();
            var req = new Request(1, "Test Digest", "12:00");
            var testdig = Crypto.CreateDigest(req);
            var ccert = new CheckpointCertificate(1, testdig,null);
            Assert.IsFalse(ccert.ProofsAreValid());
            var check1 = new Checkpoint(1, 1, testdig);
            var check2 = new Checkpoint(2, 1, testdig);
            var check3 = new Checkpoint(3, 1, testdig);
            ccert.ProofList.Add(check1);
            ccert.ProofList.Add(check2);
            ccert.ProofList.Add(check3);
            Assert.IsFalse(ccert.ProofsAreValid());
            ccert.ResetCertificate();
            Assert.IsFalse(ccert.ProofsAreValid());
            check1.SignMessage(_pri);
            check2.SignMessage(_pri);
            check3.SignMessage(_pri); 
            ccert.ProofList.Add(check1);
            ccert.ProofList.Add(check2);
            ccert.ProofList.Add(check3);
            ccert.ProofList.Add(check1);
            Assert.IsTrue(ccert.ProofsAreValid());
            ccert.ResetCertificate();
            var check1bad = new Checkpoint(1, 1, null);
            ccert.ProofList.Add(check2);
            ccert.ProofList.Add(check3);
            ccert.ProofList.Add(check1bad);
            Assert.IsFalse(ccert.ProofsAreValid());
        }

        [TestMethod]
        public void QReachedTest()
        {
            var req = new Request(1, "Test Digest", "12:00");
            var testdig = Crypto.CreateDigest(req);
            var ccert = new CheckpointCertificate(1,testdig,null);
            Assert.IsFalse(ccert.QReached(1));

            var check1 = new Checkpoint(1, 1, testdig);
            var check2 = new Checkpoint(2, 1, testdig);
            var check3 = new Checkpoint(3, 1, testdig);
            ccert.ProofList.Add(check1);
            ccert.ProofList.Add(check2);
            ccert.ProofList.Add(check3);
            Assert.IsTrue(ccert.QReached(1));
            Assert.IsFalse(ccert.QReached(2));
            ccert.ResetCertificate();
            ccert.ProofList.Add(check1);
            ccert.ProofList.Add(check2);
            var checkdup = new Checkpoint(2, 1, testdig);
            ccert.ProofList.Add(checkdup);
            Assert.IsFalse(ccert.QReached(1));
            ccert.ProofList.Add(check3);
            Assert.IsTrue(ccert.QReached(1));
        }
        
        [TestMethod]
        public void MakeLogDigestTest()
        {
            //int id, int curview, int totalreplicas, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int,string> contactList
            var checksource = new Source<CheckpointCertificate>();
            var checksource2 = new Source<CheckpointCertificate>();
            var sh = new SourceHandler(null, null, null, null, null, checksource);
            var sh2 = new SourceHandler(null, null, null, null, null, checksource2);
            var testserv = new Server(1,1,4,_scheduler,5,"127.0.0.1:9001",sh, new CDictionary<int, string>());
            var testserv2 = new Server(2,1,4,_scheduler,5,"127.0.0.1:9002",sh2, new CDictionary<int, string>());
            testserv.InitializeLog(0);
            testserv.InitializeLog(1);
            testserv.InitializeLog(2);
            testserv.InitializeLog(3);
            testserv.InitializeLog(4);
            testserv2.InitializeLog(0);
            testserv2.InitializeLog(1);
            testserv2.InitializeLog(2);
            testserv2.InitializeLog(3);
            testserv2.InitializeLog(4);
            
            var req0 = new Request(1, "Hello", "12:00");
            var req1 = new Request(1, "Hi", "12:00");
            var req2 = new Request(1, "Hola", "12:00");
            var req3 = new Request(1, "Hey you!", "12:00");
            var req4 = new Request(1, "Yo", "12:00");
            var pre0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.PrePrepare);
            var com0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.Commit);
            var pre1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.PrePrepare);
            var com1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.Commit);
            var pre2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.PrePrepare);
            var com2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.Commit);
            var pre3 = new PhaseMessage(0, 3, 1, Crypto.CreateDigest(req3), PMessageType.PrePrepare);
            var com3 = new PhaseMessage(0, 3, 1, Crypto.CreateDigest(req3), PMessageType.Commit);
            var pre4 = new PhaseMessage(0, 4, 1, Crypto.CreateDigest(req4), PMessageType.PrePrepare);
            var com4 = new PhaseMessage(0, 4, 1, Crypto.CreateDigest(req4), PMessageType.Commit);
            
            var preproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Prepared);
            var comproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Committed);
            var preproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Prepared);
            var comproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Committed);
            var preproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Prepared);
            var comproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Committed);
            var preproof3 = new ProtocolCertificate(3, 1, Crypto.CreateDigest(req3), CertType.Prepared);
            var comproof3 = new ProtocolCertificate(3, 1, Crypto.CreateDigest(req3), CertType.Committed);
            var preproof4 = new ProtocolCertificate(4, 1, Crypto.CreateDigest(req4), CertType.Prepared);
            var comproof4 = new ProtocolCertificate(4, 1, Crypto.CreateDigest(req4), CertType.Committed);
            testserv.AddProtocolCertificate(0,preproof0);
            testserv.AddProtocolCertificate(0,comproof0);
            testserv.AddProtocolCertificate(1,preproof1);
            testserv.AddProtocolCertificate(1,comproof1);
            testserv.AddProtocolCertificate(2,preproof2);
            testserv.AddProtocolCertificate(2,comproof2);
            testserv.AddProtocolCertificate(3,preproof3);
            testserv.AddProtocolCertificate(3,comproof3);
            testserv.AddProtocolCertificate(4,preproof4);
            testserv.AddProtocolCertificate(4,comproof4);
            testserv2.AddProtocolCertificate(0,preproof0);
            testserv2.AddProtocolCertificate(0,comproof0);
            testserv2.AddProtocolCertificate(1,preproof1);
            testserv2.AddProtocolCertificate(1,comproof1);
            testserv2.AddProtocolCertificate(2,preproof2);
            testserv2.AddProtocolCertificate(2,comproof2);
            testserv2.AddProtocolCertificate(3,preproof3);
            testserv2.AddProtocolCertificate(3,comproof3);
            testserv2.AddProtocolCertificate(4,preproof4);
            testserv2.AddProtocolCertificate(4,comproof4);
            byte[] dig1 = testserv.TestMakeStateDigest(5);

            preproof0.ProofList.Add(pre0);
            comproof0.ProofList.Add(com0);
            preproof1.ProofList.Add(pre1);
            comproof1.ProofList.Add(com1);
            preproof2.ProofList.Add(pre2);
            comproof2.ProofList.Add(com2);
            preproof3.ProofList.Add(pre3);
            comproof3.ProofList.Add(com3);
            preproof4.ProofList.Add(pre4);
            comproof4.ProofList.Add(com4);
            byte[] dig2 = testserv.TestMakeStateDigest(5);
            Assert.AreEqual(BitConverter.ToString(dig1),BitConverter.ToString(dig2));
            byte[] dig3 = testserv2.TestMakeStateDigest(5);
            Assert.AreEqual(BitConverter.ToString(dig1), BitConverter.ToString(dig3));
        }

        [TestMethod]
        public void ListenForStableCheckpointTest()
        {
            var checksource = new Source<CheckpointCertificate>();
            var sh = new SourceHandler(null, null, null, null, null, checksource);
            var testserv = new Server(1,1,4, _scheduler,5,"127.0.0.1:9001", sh, new CDictionary<int, string>());
            testserv.InitializeLog(0);
            testserv.InitializeLog(1);
            testserv.InitializeLog(2);

            var req0 = new Request(1, "Hello", "12:00");
            var req1 = new Request(1, "Hi", "12:00");
            var req2 = new Request(1, "Hola", "12:00");
            var pre0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.PrePrepare);
            var com0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.Commit);
            var pre1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.PrePrepare);
            var com1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.Commit);
            var pre2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.PrePrepare);
            var com2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.Commit);

            var preproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Prepared);
            var comproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Committed);
            var preproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Prepared);
            var comproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Committed);
            var preproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Prepared);
            var comproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Committed);
            preproof0.ProofList.Add(pre0);
            comproof0.ProofList.Add(com0);
            preproof1.ProofList.Add(pre1);
            comproof1.ProofList.Add(com1);
            preproof2.ProofList.Add(pre2);
            comproof2.ProofList.Add(com2);

            testserv.AddProtocolCertificate(0,preproof0);
            testserv.AddProtocolCertificate(0,comproof0);
            testserv.AddProtocolCertificate(1,preproof1);
            testserv.AddProtocolCertificate(1,comproof1);
            testserv.AddProtocolCertificate(2,preproof2);
            testserv.AddProtocolCertificate(2,comproof2);
            _ = testserv.ListenForStableCheckpoint();
            Assert.AreEqual(testserv.NrOfLogEntries(), 3);
            
            byte[] dig1 = testserv.TestMakeStateDigest(2);
            var cert1 = new CheckpointCertificate(2, dig1, testserv.EmitCheckpoint);
            testserv.CheckpointLog[2] = cert1;
            Assert.AreEqual(testserv.CheckpointLog.Count,1);
            var cp = new Checkpoint(testserv.ServID, 2, dig1);
            testserv.SignMessage(cp, MessageType.Checkpoint);
            var cp1 = new Checkpoint(0, 2, dig1);
            testserv.SignMessage(cp1, MessageType.Checkpoint);
            var cp2 = new Checkpoint(2, 2, dig1);
            testserv.SignMessage(cp2, MessageType.Checkpoint);
            cert1.ProofList.Add(cp);
            cert1.ProofList.Add(cp1);
            cert1.ProofList.Add(cp2);
            cert1.Stable = true;
            Assert.AreEqual(testserv.StableCheckpointsCertificate, null);
            Assert.IsTrue(cert1.ValidateCertificate(1));
            //cert1.AppendProof(cp, testserv.Pubkey,1);
            cert1.EmitCheckpoint(cert1);
            Thread.Sleep(1000);
            Assert.AreEqual(testserv.NrOfLogEntries(),0);
            Assert.AreEqual(testserv.CheckpointLog.Count,0);
            Console.WriteLine(testserv.StableCheckpointsCertificate);
            Assert.AreNotEqual(testserv.StableCheckpointsCertificate,null);
        }

        [TestMethod]
        public void AppendCheckpointCertificateTest()
        {
            var checksource = new Source<CheckpointCertificate>();
            var sh = new SourceHandler(null, null, null, null, null, checksource);
            var testserv = new Server(1,1,4,_scheduler,5,"127.0.0.1:9001", sh,new CDictionary<int, string>());
            testserv.InitializeLog(0);
            testserv.InitializeLog(1);
            testserv.InitializeLog(2);
            
            var req0 = new Request(1, "Hello", "12:00");
            var req1 = new Request(1, "Hi", "12:00");
            var req2 = new Request(1, "Hola", "12:00");
            var pre0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.PrePrepare);
            var com0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.Commit);
            var pre1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.PrePrepare);
            var com1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.Commit);
            var pre2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.PrePrepare);
            var com2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.Commit);

            var preproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Prepared);
            var comproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Committed);
            var preproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Prepared);
            var comproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Committed);
            var preproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Prepared);
            var comproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Committed);
            preproof0.ProofList.Add(pre0);
            comproof0.ProofList.Add(com0);
            preproof1.ProofList.Add(pre1);
            comproof1.ProofList.Add(com1);
            preproof2.ProofList.Add(pre2);
            comproof2.ProofList.Add(com2);

            testserv.AddProtocolCertificate(0,preproof0);
            testserv.AddProtocolCertificate(0,comproof0);
            testserv.AddProtocolCertificate(1,preproof1);
            testserv.AddProtocolCertificate(1,comproof1);
            testserv.AddProtocolCertificate(2,preproof2);
            testserv.AddProtocolCertificate(2,comproof2);
            _ = testserv.ListenForStableCheckpoint();
            Assert.AreEqual(testserv.NrOfLogEntries(), 3);
            
            byte[] dig1 = testserv.TestMakeStateDigest(2);
            var cert1 = new CheckpointCertificate(2, dig1, testserv.EmitCheckpoint);
            testserv.CheckpointLog[2] = cert1;
            Assert.AreEqual(testserv.CheckpointLog.Count,1);
            var cp = new Checkpoint(testserv.ServID, 2, dig1);
            testserv.SignMessage(cp, MessageType.Checkpoint);
            var cp1 = new Checkpoint(0, 2, dig1);
            testserv.SignMessage(cp1, MessageType.Checkpoint);
            var cp2 = new Checkpoint(2, 2, dig1);
            testserv.SignMessage(cp2, MessageType.Checkpoint);
            cert1.AppendProof(cp, testserv.Pubkey, 1);
            cert1.AppendProof(cp1, testserv.Pubkey, 1);
            cert1.AppendProof(cp2, testserv.Pubkey, 1);
            Assert.IsTrue(cert1.ProofsAreValid());
            Assert.AreEqual(cert1.ProofList.Count,3);
            Thread.Sleep(1000);
            Assert.AreEqual(testserv.NrOfLogEntries(),0);
            Assert.AreEqual(testserv.CheckpointLog.Count,0);
            Console.WriteLine(testserv.StableCheckpointsCertificate);
            Assert.AreNotEqual(testserv.StableCheckpointsCertificate,null);
        }

        [TestMethod]
        public void FaultyCheckpointValidationTest()
        {
            var checksource = new Source<CheckpointCertificate>();
            var sh = new SourceHandler(null, null, null, null, null, checksource);
             var testserv = new Server(1,1,4, _scheduler,5,"127.0.0.1:9001",sh, new CDictionary<int, string>());
            testserv.InitializeLog(0);
            testserv.InitializeLog(1);
            testserv.InitializeLog(2);
            
            var req0 = new Request(1, "Hello", "12:00");
            var req1 = new Request(1, "Hi", "12:00");
            var req2 = new Request(1, "Hola", "12:00");
            var pre0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.PrePrepare);
            var com0 = new PhaseMessage(0, 0, 1, Crypto.CreateDigest(req0), PMessageType.Commit);
            var pre1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.PrePrepare);
            var com1 = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req1), PMessageType.Commit);
            var pre2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.PrePrepare);
            var com2 = new PhaseMessage(0, 2, 1, Crypto.CreateDigest(req2), PMessageType.Commit);

            var preproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Prepared);
            var comproof0 = new ProtocolCertificate(0, 1, Crypto.CreateDigest(req0), CertType.Committed);
            var preproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Prepared);
            var comproof1 = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req1), CertType.Committed);
            var preproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Prepared);
            var comproof2 = new ProtocolCertificate(2, 1, Crypto.CreateDigest(req2), CertType.Committed);
            preproof0.ProofList.Add(pre0);
            comproof0.ProofList.Add(com0);
            preproof1.ProofList.Add(pre1);
            comproof1.ProofList.Add(com1);
            preproof2.ProofList.Add(pre2);
            comproof2.ProofList.Add(com2);

            testserv.AddProtocolCertificate(0,preproof0);
            testserv.AddProtocolCertificate(0,comproof0);
            testserv.AddProtocolCertificate(1,preproof1);
            testserv.AddProtocolCertificate(1,comproof1);
            testserv.AddProtocolCertificate(2,preproof2);
            testserv.AddProtocolCertificate(2,comproof2);
            _ = testserv.ListenForStableCheckpoint();
            Assert.AreEqual(testserv.NrOfLogEntries(), 3);
            
            byte[] dig1 = testserv.TestMakeStateDigest(2);
            var cert1 = new CheckpointCertificate(2, dig1, testserv.EmitCheckpoint);
            testserv.CheckpointLog[2] = cert1;
            Assert.AreEqual(testserv.CheckpointLog.Count,1);
            var cp = new Checkpoint(testserv.ServID, 2, dig1);
            testserv.SignMessage(cp, MessageType.Checkpoint);
            var cp1 = new Checkpoint(0, 2, dig1);
            testserv.SignMessage(cp1, MessageType.Checkpoint);
            var cp2 = new Checkpoint(2, 2, null);
            testserv.SignMessage(cp2, MessageType.Checkpoint);
            cert1.AppendProof(cp, testserv.Pubkey, 1);
            cert1.AppendProof(cp1, testserv.Pubkey, 1);
            cert1.AppendProof(cp2, testserv.Pubkey, 1);
            Assert.IsFalse(cert1.ValidateCertificate(1));
            Assert.AreEqual(testserv.NrOfLogEntries(), 3);
            Assert.AreEqual(testserv.CheckpointLog.Count,1);
            Assert.AreEqual(testserv.CheckpointLog[2].LastSeqNr,cert1.LastSeqNr);
            Assert.AreEqual(testserv.CheckpointLog[2].Stable,cert1.Stable);
            Assert.IsTrue(testserv.CheckpointLog[2].StateDigest.SequenceEqual(cert1.StateDigest));
        }
    }
}