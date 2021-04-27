using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;
using PBFT.Replica.Protocol;

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
            Source<bool> shutbridge = new Source<bool>();
            Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
            Server testserv = new Server(0,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv, 1, pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
            Request req = new Request(1, "Hello World!", "12:00");
            req.SignMessage(_prikey);
            var reply = PerformTestFunction(exec, testserv ,req, pesbridge, _scheduler).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }

        [TestMethod]
        public void ProtocolExecutionNoPrimaryNoPersistencyTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            Source<bool> shutbridge = new Source<bool>();
            Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
            Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, null, shutdownPhase, new Source<bool>() ,new Source<NewView>(), shutbridge);
            Request req = new Request(1, "Hello Galaxy!", "12:00");
            req.SignMessage(_prikey);
            var reply = PerformTestFunction(exec, testserv, req, pesbridge, _scheduler).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }
        
        public async Task<Reply> PerformTestFunction(ProtocolExecution exec, Server serv, Request req, Source<PhaseMessage> pmesbridge, Engine scheduler)
        {
            var digest = Crypto.CreateDigest(req);
            var cancel = new CancellationTokenSource();
            exec.Active = true;
            serv.ProtocolActive = true;
            var protocol = exec.HandleRequestTest(req, 1, cancel); //Protocol starting
            
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
            await scheduler.Schedule(() =>
            {
                //serv.ServPubKeyRegister[3] = pubkey3;
                pmesbridge.Emit(pm1);
                pmesbridge.Emit(pm2);
                //Thread.Sleep(3000);
                pmesbridge.Emit(pm3);
                pmesbridge.Emit(pm4);    
            });
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
            Source<bool> shutbridge = new Source<bool>();
            Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
            Server testserv = new Server(0,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1,pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
            Request req = new Request(1, "Hello World!", "12:00");
            req.SignMessage(_prikey);
            var reply = PerformTestWrongOrderFunction(exec, testserv ,req, pesbridge, _scheduler).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }
        
        [TestMethod]
        public void ProtocolExecutionNoPrimaryNoPersistencyWrongOrderTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            Source<bool> shutbridge = new Source<bool>();
            Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
            Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
            Request req = new Request(1, "Hello Galaxy!", "12:00");
            req.SignMessage(_prikey);
            var reply = PerformTestWrongOrderFunction(exec, testserv, req, pesbridge, _scheduler).GetAwaiter().GetResult();
            StringAssert.Contains(reply.Result, req.Message);
        }
        
        public async Task<Reply> PerformTestWrongOrderFunction(ProtocolExecution exec, Server serv, Request req, Source<PhaseMessage> pmesbridge, Engine scheduler)
        {
            var digest = Crypto.CreateDigest(req);
            CancellationTokenSource cancel = new CancellationTokenSource();
            exec.Active = true;
            serv.ProtocolActive = true;
            var protocol = exec.HandleRequestTest(req, 1, cancel); //Protocol starting
            
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

            await scheduler.Schedule(() =>
            {
                pmesbridge.Emit(pm1);
                pmesbridge.Emit(pm3);
                pmesbridge.Emit(pm2);
                pmesbridge.Emit(pm4);
            });
           
            var rep = await protocol;
            Console.WriteLine("Reply: " + rep);
            return rep;
        }
        
        [TestMethod]
        public void TimeoutTest()
        {
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            Source<bool> shutbridge = new Source<bool>();
            Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
            Server testserv = new Server(1,0,4,_scheduler,20,"127.0.0.1:9000",sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
            Request req = new Request(1, "Hello Galaxy!", "12:00");
            req.SignMessage(_prikey);
            PerformTestFunctionTimeout(exec, testserv, req, pesbridge).GetAwaiter().OnCompleted(() => Console.WriteLine("Test"));
            //Console.WriteLine(reply);
            //StringAssert.Contains(reply.Result, req.Message);
            Thread.Sleep(5000);
        }
        
        public async CTask<Reply> PerformTestFunctionTimeout(ProtocolExecution exec, Server serv, Request req, Source<PhaseMessage> pmesbridge)
        {
            var digest = Crypto.CreateDigest(req);
            CancellationTokenSource cancel = new CancellationTokenSource();
            exec.Active = true;
            serv.ProtocolActive = true;
            var protocol = exec.HandleRequestTest(req, 1, cancel); //Protocol starting
            
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
        
        [TestMethod]
       public void ProtocolExecutionWithAnyTimeoutFunctionalityForResultTest()
       {
            CList<string> aoo = new CList<string>();
            var (_prikey, _) = Crypto.InitializeKeyPairs();
            Source<Request> reqbridge = new Source<Request>();
            Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
            Source<bool> shutbridge = new Source<bool>();
            Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
            var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
            Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
            ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
            Request req = new Request(1, "Hello Galaxy!", "12:00");
            req.SignMessage(_prikey);
            var prot = PerformTestWithAnyTest(exec, testserv, _scheduler, req, true, pesbridge, shutbridge, shutdownPhase).GetAwaiter();
            Thread.Sleep(10000);
            var res = prot.GetResult();
            Assert.IsTrue(res);
       }

       [TestMethod]
       public void ProtocolExecutionWithAnyTimeoutFunctionalityForTimeoutTest()
       {
           var (_prikey, _) = Crypto.InitializeKeyPairs();
           Source<Request> reqbridge = new Source<Request>();
           Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
           Source<bool> shutbridge = new Source<bool>();
           Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
           var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
           Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
           ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
           Request req = new Request(1, "Hello Galaxy!", "12:00");
           req.SignMessage(_prikey);
           var prot = PerformTestWithAnyTest(exec, testserv, _scheduler, req, false, pesbridge, shutbridge, shutdownPhase).GetAwaiter();
           Thread.Sleep(10000);
           var res = prot.GetResult();
           Assert.IsFalse(res);
       }
       
       public async CTask<bool> PerformTestWithAnyTest(ProtocolExecution exec, Server testserver, Engine sche, Request req, bool emit,  Source<PhaseMessage> phasesource, Source<bool> shutdownsource, Source<PhaseMessage> shutdownphase)
       {
           exec.Active = true;
           testserver.ProtocolActive = true;
           CancellationTokenSource cancel = new CancellationTokenSource();
           _ = TimeoutOps.AbortableProtocolTimeoutOperation(shutdownsource, 9000, cancel.Token, sche);
           if (emit) _ = CreateEmitMessages(sche, phasesource, testserver, req);
           var res = await WhenAny<bool>.Of(PerformHandleRequest(exec, req, cancel), ListenForShutdown(shutdownsource, shutdownphase));
           return res;
       }

       public async CTask<bool> PerformHandleRequest(ProtocolExecution exec, Request req, CancellationTokenSource cancel)
       {
           var reply = await exec.HandleRequestTest(req, 1, cancel);
           Console.WriteLine("Received Reply");
           var copyreply = (Reply) reply.CreateCopyTemplate();
           var solutionreply = new Reply(1, 1, 0, true, "Hello Galaxy!", "12:00");
           return copyreply.Compare(solutionreply);
       }

       public async CTask<bool> ListenForShutdown(Source<bool> shutdown, Source<PhaseMessage> shutdownphase)
       {
           var res = await shutdown.Next();
           Console.WriteLine("Shutdown obtained");
           shutdownphase.Emit(new PhaseMessage(-1,-1,-1,null,PMessageType.End));
           return res;
       }

       public async Task CreateEmitMessages(Engine sche, Source<PhaseMessage> phaseSource, Server serv, Request req)
       {
           var (prikey1, pubkey1) = Crypto.InitializeKeyPairs();
           var (prikey2, pubkey2) = Crypto.InitializeKeyPairs();
           var (prikey3, pubkey3) = Crypto.InitializeKeyPairs();
           var digest = Crypto.CreateDigest(req);
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
           Console.WriteLine("Scheduling and emitting the protocol messages");
           await Task.Delay(3000);
           await sche.Schedule(() =>
           {
               phaseSource.Emit(pm1);
               phaseSource.Emit(pm2);
               //Thread.Sleep(3000);
               phaseSource.Emit(pm3);
               phaseSource.Emit(pm4);    
           });
       }

       [TestMethod]
       public void PerformShutdownForViewChangeMessages()
       {
           var (_prikey, _) = Crypto.InitializeKeyPairs();
           Source<Request> reqbridge = new Source<Request>();
           Source<PhaseMessage> pesbridge = new Source<PhaseMessage>();
           Source<bool> shutbridge = new Source<bool>();
           Source<PhaseMessage> shutdownPhase = new Source<PhaseMessage>();
           var sh = new SourceHandler(reqbridge, pesbridge, null, shutbridge, null, null, null, null, null);
           Server testserv = new Server(1,0,4, _scheduler,20,"127.0.0.1:9000", sh, new CDictionary<int, string>());
           ProtocolExecution exec = new ProtocolExecution(testserv,1, pesbridge, null, shutdownPhase, new Source<bool>(), new Source<NewView>(), shutbridge);
           Request req = new Request(1, "Hello Galaxy!", "12:00");
           ViewPrimary nextvp = new ViewPrimary(4);
           nextvp.NextPrimary();
           ViewChangeCertificate vcc = new ViewChangeCertificate(nextvp, null, testserv.EmitShutdown , testserv.EmitViewChange);
           req.SignMessage(_prikey);
           var prot = PerformTestWithAnyTest2(exec, testserv, _scheduler, req, true, vcc, pesbridge, shutbridge, shutdownPhase).GetAwaiter();
           Thread.Sleep(10000);
           var res = prot.GetResult();
           Assert.IsFalse(res);
       }
       
       public async CTask<bool> PerformTestWithAnyTest2(ProtocolExecution exec, Server testserv, Engine sche, Request req, bool emit, ViewChangeCertificate vcc, Source<PhaseMessage> phasesource, Source<bool> shutdownsource, Source<PhaseMessage> shutdownphase)
       {
           var (prikey1, pubkey1) = Crypto.InitializeKeyPairs();
           var (prikey2, pubkey2) = Crypto.InitializeKeyPairs();
           var (prikey3, pubkey3) = Crypto.InitializeKeyPairs();
           if (testserv.IsPrimary()) testserv.ServPubKeyRegister[1] = pubkey1;
           else testserv.ServPubKeyRegister[0] = pubkey1;
           testserv.ServPubKeyRegister[2] = pubkey2;
           testserv.ServPubKeyRegister[3] = pubkey3;
           exec.Active = true;
           testserv.ProtocolActive = true;
           CancellationTokenSource cancel = new CancellationTokenSource();
           _ = TimeoutOps.AbortableProtocolTimeoutOperation(shutdownsource, 9000, cancel.Token, sche);
           if (emit) _ = CreateEmitMessagesIncomplete(sche, phasesource, testserv, req, prikey1, prikey2, prikey3);
           _ = FillUpViewChangeCert(vcc, prikey1, prikey2, pubkey1, pubkey2);
           var res = await WhenAny<bool>.Of(PerformHandleRequest(exec, req, cancel), ListenForShutdown(shutdownsource, shutdownphase));
           return res;
       }

       public async CTask FillUpViewChangeCert(ViewChangeCertificate vcc, RSAParameters prikey1, RSAParameters prikey2, RSAParameters pubkey1, RSAParameters pubkey2)
       {
           await Task.Delay(2000);
           ViewChange vc1 = new ViewChange(-1, 0, 1, null, new CDictionary<int, ProtocolCertificate>());
           ViewChange vc2 = new ViewChange(-1, 2, 1, null, new CDictionary<int, ProtocolCertificate>());
           vc1.SignMessage(prikey1);
           vc2.SignMessage(prikey2);
           vcc.AppendViewChange(vc1, pubkey1, 1);
           vcc.AppendViewChange(vc2, pubkey2, 1);
       }
       
       public async Task CreateEmitMessagesIncomplete(Engine sche, Source<PhaseMessage> phaseSource, Server serv, Request req, RSAParameters prikey1, RSAParameters prikey2, RSAParameters prikey3)
       {
           var digest = Crypto.CreateDigest(req);
           PhaseMessage pm1;
           Console.WriteLine(serv.IsPrimary());
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
           Console.WriteLine("Scheduling and emitting the protocol messages");
           await Task.Delay(3000);
           //not emitting so timeout can occur
       }
    }
}