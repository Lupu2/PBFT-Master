using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica.Protocol;

namespace PBFT.Tests.Replica.Protocol
{
    [TestClass]
    public class CheckpointListenerTests
    {
        private Engine _scheduler;
        
        [TestInitialize]
        public void SchedulerInitializer()
        {
            var storage = new InMemoryStorageEngine();
            _scheduler = ExecutionEngineFactory.StartNew(storage);
        }
        
        [TestMethod]
        public void CheckpointListenerBasicTest()
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
            Source<Checkpoint> checkbridge = new Source<Checkpoint>();
            var dig = Crypto.CreateDigest(new Request(1, "12:00"));
            var checklistener = new CheckpointListener(
                5, 
                Quorum.CalculateFailureLimit(4), 
                dig, 
                checkbridge
            );
            
            var checkcert = new CheckpointCertificate(5, dig, null);
            var check1 = new Checkpoint(0, 5, dig);
            check1.SignMessage(pri0);
            var check2 = new Checkpoint(1, 5, dig);
            check2.SignMessage(pri1);
            var check3 = new Checkpoint(2, 5, dig);
            check3.SignMessage(pri2);
            var check4 = new Checkpoint(3, 5, dig);
            check4.SignMessage(pri3);
            checklistener.Listen(checkcert, keys, ListenForEmit);
            
            _scheduler.Schedule(() =>
            {
                checkbridge.Emit(check1);
                Thread.Sleep(500);
                checkbridge.Emit(check2);
                Thread.Sleep(500);
                checkbridge.Emit(check3);
            });
            Thread.Sleep(3000);
            Assert.AreEqual(checkcert.ProofList.Count, 3);
            Assert.IsTrue(checkcert.Stable);
            Console.WriteLine("Normal thread assertions finished");
        }

        public void ListenForEmit(CheckpointCertificate checkcert)
        {
            Console.WriteLine("ListenForEmit");
            Assert.AreEqual(checkcert.ProofList.Count, 3);
            Assert.IsTrue(checkcert.Stable);
            Console.WriteLine("ListenForEmit is finished");
        }
    }
}