using System;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests
{
    [TestClass]
    public class ProtocolTests
    {
        private Engine _scheduler;
        
        [TestInitialize]
        public void SchedulerInitialize()
        {
            var storage = new InMemoryStorageEngine();
            _scheduler = ExecutionEngineFactory.StartNew(storage);
        }
        
        [TestMethod]
        public void ProtocolExecutionPrimaryNoPersistencyTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, new Source<ViewChangeCertificate>(), null, null);
            Server testserv = new Server(0,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv, 1, pesbridge, new Source<bool>(), new Source<NewView>());
            Request req = new Request(1, "Hello World!", DateTime.Now.ToString());
            req.SignMessage(_prikey);
            var reply = PerformTestFunction(exec, testserv ,req, pesbridge).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }

        [TestMethod]
        public void ProtocolExecutionNoPrimaryNoPersistencyTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, new Source<ViewChangeCertificate>(), null, null);
            Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, new Source<bool>() ,new Source<NewView>());
            Request req = new Request(1, "Hello Galaxy!", DateTime.Now.ToString());
            req.SignMessage(_prikey);
            var reply = PerformTestFunction(exec, testserv, req, pesbridge).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }
        
        public async Task<Reply> PerformTestFunction(ProtocolExecution exec, Server serv, Request req, Source<PhaseMessage> pmesbridge)
        {
            var digest = Crypto.CreateDigest(req);
            var protocol = exec.HandleRequestTest(req, 1); //Protocol starting
            
            //Key initialization
            var (prikey1, pubkey1) = Crypto.InitializeKeyPairs();
            var (prikey2, pubkey2) = Crypto.InitializeKeyPairs();
            var (prikey3, pubkey3) = Crypto.InitializeKeyPairs();
            
            //Message initialization
            PhaseMessage pm1;
            if (serv.IsPrimary()) pm1 = new PhaseMessage(1, 1, 0, digest, PMessageType.Prepare);
            else pm1 = new PhaseMessage(0, 1, 0, digest, PMessageType.PrePrepare);
            pm1.SignMessage(prikey1);
            var pm2 = new PhaseMessage(2, 1, 0, digest, PMessageType.Prepare);
            pm2.SignMessage(prikey2);
            PhaseMessage pm3;
            if (serv.IsPrimary()) pm3 = new PhaseMessage(1, 1, 0, digest, PMessageType.Commit);
            else pm3 = new PhaseMessage(0, 1, 0, digest, PMessageType.Commit);
            pm3.SignMessage(prikey1);
            var pm4 = new PhaseMessage(2, 1, 0, digest, PMessageType.Commit);
            pm4.SignMessage(prikey2);
            var pm5 = new PhaseMessage(3, 1, 0, digest, PMessageType.Commit);
            pm5.SignMessage(prikey3);
            if (serv.IsPrimary()) serv.ServPubKeyRegister[1] = pubkey1;
            else serv.ServPubKeyRegister[0] = pubkey1;
            serv.ServPubKeyRegister[2] = pubkey2;
            //serv.ServPubKeyRegister[3] = pubkey3;
            pmesbridge.Emit(pm1);
            pmesbridge.Emit(pm2);
            //Thread.Sleep(3000);
            pmesbridge.Emit(pm3);
            pmesbridge.Emit(pm4);
            //pmesbridge.Emit(pm5);
            var rep = await protocol;
            return rep;
        }
        
        [TestMethod]
        public void ProtocolExecutionPrimaryNoPersistencyWrongOrderTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, new Source<ViewChangeCertificate>(), null, null);
            Server testserv = new Server(0,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1,pesbridge,  new Source<bool>(), new Source<NewView>());
            Request req = new Request(1, "Hello World!", DateTime.Now.ToString());
            req.SignMessage(_prikey);
            var reply = PerformTestWrongOrderFunction(exec, testserv ,req, pesbridge).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }
        
        [TestMethod]
        public void ProtocolExecutionNoPrimaryNoPersistencyWrongOrderTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, new Source<ViewChangeCertificate>(), null, null);
            Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, new Source<bool>(), new Source<NewView>());
            Request req = new Request(1, "Hello Galaxy!", DateTime.Now.ToString());
            req.SignMessage(_prikey);
            var reply = PerformTestWrongOrderFunction(exec, testserv, req, pesbridge).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }
        
        public async Task<Reply> PerformTestWrongOrderFunction(ProtocolExecution exec, Server serv, Request req, Source<PhaseMessage> pmesbridge)
        {
            var digest = Crypto.CreateDigest(req);
            var protocol = exec.HandleRequestTest(req, 1); //Protocol starting
            
            //Key initialization
            var (prikey1, pubkey1) = Crypto.InitializeKeyPairs();
            var (prikey2, pubkey2) = Crypto.InitializeKeyPairs();
            
            //Message initialization
            PhaseMessage pm1;
            if (serv.IsPrimary()) pm1 = new PhaseMessage(1, 1, 0, digest, PMessageType.Prepare);
            else pm1 = new PhaseMessage(0, 1, 0, digest, PMessageType.PrePrepare);
            pm1.SignMessage(prikey1);
            var pm2 = new PhaseMessage(2, 1, 0, digest, PMessageType.Prepare);
            pm2.SignMessage(prikey2);
            PhaseMessage pm3;
            if (serv.IsPrimary()) pm3 = new PhaseMessage(1, 1, 0, digest, PMessageType.Commit);
            else pm3 = new PhaseMessage(0, 1, 0, digest, PMessageType.Commit);
            pm3.SignMessage(prikey1);
            var pm4 = new PhaseMessage(2, 1, 0, digest, PMessageType.Commit);
            pm4.SignMessage(prikey2);
            
            if (serv.IsPrimary()) serv.ServPubKeyRegister[1] = pubkey1;
            else serv.ServPubKeyRegister[0] = pubkey1;
            serv.ServPubKeyRegister[2] = pubkey2;
            
            pmesbridge.Emit(pm1);
            pmesbridge.Emit(pm3);
            pmesbridge.Emit(pm2);
            pmesbridge.Emit(pm4);
            var rep = await protocol;
            Console.WriteLine("Reply: " + rep);
            return rep;
        }

        [TestMethod]
        public void ProtocolTestWithFullWorkflow()
        {
            
        }

        [TestMethod]
        public void TimeoutTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, new Source<ViewChangeCertificate>(), null, null);
            Server testserv = new Server(1,0,4,_scheduler,20,"127.0.0.1:9000",sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, new Source<bool>(), new Source<NewView>());
            Request req = new Request(1, "Hello Galaxy!", DateTime.Now.ToString());
            req.SignMessage(_prikey);
            PerformTestFunctionTimeout(exec, testserv, req, pesbridge).GetAwaiter().OnCompleted(() => Console.WriteLine("Test"));
            //Console.WriteLine(reply);
            //StringAssert.Contains(reply.Result, req.Message);
            Thread.Sleep(5000);
        }
        
        public async CTask<Reply> PerformTestFunctionTimeout(ProtocolExecution exec, Server serv, Request req, Source<PhaseMessage> pmesbridge)
        {
            var digest = Crypto.CreateDigest(req);
            var protocol = exec.HandleRequestTest(req, 1); //Protocol starting
            
            //Key initialization
            var (prikey1, pubkey1) = Crypto.InitializeKeyPairs();
            var (prikey2, pubkey2) = Crypto.InitializeKeyPairs();
            var (prikey3, pubkey3) = Crypto.InitializeKeyPairs();
            
            //Message initialization
            PhaseMessage pm1;
            if (serv.IsPrimary()) pm1 = new PhaseMessage(1, 1, 0, digest, PMessageType.Prepare);
            else pm1 = new PhaseMessage(0, 1, 0, digest, PMessageType.PrePrepare);
            pm1.SignMessage(prikey1);
            var pm2 = new PhaseMessage(2, 1, 0, digest, PMessageType.Prepare);
            pm2.SignMessage(prikey2);
            PhaseMessage pm3;
            if (serv.IsPrimary()) pm3 = new PhaseMessage(1, 1, 0, digest, PMessageType.Commit);
            else pm3 = new PhaseMessage(0, 1, 0, digest, PMessageType.Commit);
            pm3.SignMessage(prikey1);
            var pm4 = new PhaseMessage(2, 1, 0, digest, PMessageType.Commit);
            pm4.SignMessage(prikey2);
            var pm5 = new PhaseMessage(3, 1, 0, digest, PMessageType.Commit);
            pm5.SignMessage(prikey3);
            if (serv.IsPrimary()) serv.ServPubKeyRegister[1] = pubkey1;
            else serv.ServPubKeyRegister[0] = pubkey1;
            serv.ServPubKeyRegister[2] = pubkey2;
            serv.ServPubKeyRegister[3] = pubkey3;
            Thread.Sleep(1000);
            pmesbridge.Emit(pm1);
            pmesbridge.Emit(pm2);
            //Thread.Sleep(3000);
            pmesbridge.Emit(pm3);
            pmesbridge.Emit(pm4);
            //pmesbridge.Emit(pm5);
            var rep = await protocol;
            return rep;
        }
    }
}