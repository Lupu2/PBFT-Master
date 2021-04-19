using System;
using System.IO;
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT
{
    public static class App
    {
        public static CList<string> PseudoApp;

        public static void PseudoAppContent()
        {
            foreach (var con in PseudoApp)
                Console.WriteLine(con);
        }
        public static void Run(string[] args)
        {
            Console.WriteLine("Application running...");
            Console.WriteLine(args.Length);
            Console.WriteLine(Directory.GetCurrentDirectory());
            if (args.Length > 0) //add arguments by editing configuration program arguments or by adding parameters behind executable directly
            {
                Console.WriteLine("arguments:");
                foreach (var arg in args) Console.WriteLine(arg);
                
                //Format id=id test=true/false
                Console.WriteLine(args[0].Split("id=")[1]);
                int paramid = Int32.Parse(args[0].Split("id=")[1]);
                bool testparam = Boolean.Parse(args[1].Split("test=")[1]);
                bool usememory = Boolean.Parse(args[2].Split("per=")[1]);
                var storageEngine = new SimpleFileStorageEngine("PBFTStorage"+paramid+".txt", !usememory); //change to false when done debugging
                (int, string) servInfo;
                CDictionary<int, string> serversInfo;
                Console.WriteLine(paramid);
                Console.WriteLine(testparam);
                if (testparam) servInfo = LoadJSONValues.GetServerData("testServerInfo.json",paramid).GetAwaiter().GetResult();
                else servInfo = LoadJSONValues.GetServerData("serverInfo.json",paramid).GetAwaiter().GetResult();

                var id = servInfo.Item1;
                var ipaddr = servInfo.Item2;
                Console.WriteLine("Result");
                Console.WriteLine(id);
                Console.WriteLine(ipaddr);
                if (testparam) serversInfo = LoadJSONValues.LoadJSONFileContent("testServerInfo.json").Result;
                else serversInfo = LoadJSONValues.LoadJSONFileContent("serverInfo.json").Result;
                //var con = File.Exists("./PBFTStorage.txt");
                var con = File.Exists("./PBFTStorage" + id + ".txt");
                Console.WriteLine("Found storage:" + con);
                Console.WriteLine(con);
                Engine scheduler;
                Server server = null;
                ProtocolExecution protexec = null;
                if (!con || !usememory)
                {
                    Source<Request> reqSource = new Source<Request>();
                    Source<PhaseMessage> protSource = new Source<PhaseMessage>();
                    Source<PhaseMessage> redistSource = new Source<PhaseMessage>();
                    Source<ViewChange> viewSource = new Source<ViewChange>();
                    Source<Checkpoint> checkSource = new Source<Checkpoint>();
                    Source<bool> viewfinSource = new Source<bool>();
                    Source<bool> shutdownfinSource = new Source<bool>();
                    Source<PhaseMessage> shutdownPhaseSource = new Source<PhaseMessage>();
                    Source<NewView> newviewSource = new Source<NewView>();
                    Source<CheckpointCertificate> checkfinSource = new Source<CheckpointCertificate>();
                    SourceHandler sh = new (
                        reqSource, 
                        protSource, 
                        viewSource,
                        viewfinSource, 
                        shutdownfinSource, 
                        newviewSource, 
                        redistSource, 
                        checkSource,
                        checkfinSource
                    );
                    PseudoApp = new CList<string>();
                    Console.WriteLine("Starting application");
                    scheduler = ExecutionEngineFactory.StartNew(storageEngine);
                    server = new Server(id, 0, serversInfo.Count, scheduler, 5, ipaddr, sh, serversInfo);
                    server.Start();
                    Thread.Sleep(1000);
                    protexec = new ProtocolExecution(
                        server, 
                        1, 
                        protSource, 
                        redistSource, 
                        shutdownPhaseSource, 
                        viewfinSource, 
                        newviewSource, 
                        shutdownfinSource
                    );
                    scheduler.Schedule(() =>
                    {
                        Roots.Entangle(PseudoApp);
                        Roots.Entangle(protexec);
                    });
                    scheduler.Sync().Wait();
                    server.InitializeConnections()
                        .GetAwaiter()
                        .OnCompleted(() =>
                            StartRequestHandler(protexec, reqSource, shutdownPhaseSource, scheduler)
                        );
                }
                else
                {
                    //load persistent data
                    Console.WriteLine("Restarting application");
                    scheduler = ExecutionEngineFactory.Continue(storageEngine);
                    scheduler.Schedule(() =>
                    {
                        //server = Roots.Resolve<Server>();
                        PseudoApp = Roots.Resolve<CList<string>>();
                        protexec = Roots.Resolve<ProtocolExecution>();
                        server = protexec.Serv;
                    }).GetAwaiter().OnCompleted(() =>
                    {
                        server.AddEngine(scheduler);
                        server.SeeLog();
                        PseudoAppContent();
                        server.Start();
                        Thread.Sleep(1000);
                        server.InitializeConnections()
                            .GetAwaiter()
                            .OnCompleted(() => 
                                StartRequestHandler(protexec, server.Subjects.RequestSubject, protexec.ShutdownBridgePhase, scheduler)
                        );
                    });
                }
                Console.ReadLine();
                server.Dispose();
            }
        }
        
        public static void StartRequestHandler(ProtocolExecution execute, Source<Request> requestMessage, Source<PhaseMessage> shutdownPhase, Engine scheduler)
        {
            _ = RequestHandler(execute, requestMessage, shutdownPhase, scheduler);
        }
        
        public static async CTask RequestHandler(ProtocolExecution execute, Source<Request> requestMessage, Source<PhaseMessage> shutdownPhaseSource, Engine scheduler)
        {
            Console.WriteLine("RequestHandler");
            Server serv = execute.Serv;
            serv.ProtocolActive = true;
            while (true)
            {
                //TODO redesign to operate based on seqNr instead of req!
                var req = await requestMessage.Next();
                Console.WriteLine("Received Client Request");
                if (Crypto.VerifySignature(req.Signature, req.CreateCopyTemplate().SerializeToBuffer(), serv.ClientPubKeyRegister[req.ClientID]) && serv.CurSeqNr < serv.CurSeqRange.End.Value)
                {
                    if (execute.Active)
                    {
                        Console.WriteLine("Handling client request");
                        CancellationTokenSource cancel = new CancellationTokenSource();
                        _ = TimeoutOps.AbortableProtocolTimeoutOperation(
                            serv.Subjects.ShutdownSubject, 
                            10000,
                            cancel.Token,
                            scheduler
                        );
                        int seq = ++serv.CurSeqNr;
                        execute.Serv.ChangeClientStatus(req.ClientID);
                        
                        bool res = await WhenAny<bool>.Of(AppOperation(req, serv, execute, seq, cancel),
                            ListenForShutdown(serv.Subjects.ShutdownSubject));
                        Console.WriteLine("Result: " + res);
                        if (res)
                        {
                            Console.WriteLine("APP OPERATION FINISHED");
                            execute.Serv.ChangeClientStatus(req.ClientID);
                            if (seq % serv.CheckpointConstant == 0 && serv.CurSeqNr != 0) //really shouldn't call this at seq nr 0, but just incase
                                //serv.CreateCheckpoint(execute.Serv.CurSeqNr, PseudoApp);
                                serv.CreateCheckpoint2(execute.Serv.CurSeqNr, PseudoApp);
                            Console.WriteLine("FINISHED TASK");
                            //await scheduler.Sync(); //doesn't work properly , Wait() causes inf-loop
                            await Sync.Next();
                            Console.WriteLine("Finished Sync");
                        }
                        else
                        {
                            Console.WriteLine("View-Change commence :)");
                            execute.Active = false;
                            serv.ProtocolActive = false;
                            await scheduler.Schedule(() =>
                                shutdownPhaseSource.Emit(new PhaseMessage(-1, -1, -1, null, PMessageType.End)
                                ));
                            await execute.HandlePrimaryChange2();
                            Console.WriteLine("View-Change completed");
                            serv.UpdateSeqNr();
                            if (serv.CurSeqNr % serv.CheckpointConstant == 0 && serv.CurSeqNr != 0) 
                                //serv.CreateCheckpoint(execute.Serv.CurSeqNr, PseudoApp);
                                serv.CreateCheckpoint2(execute.Serv.CurSeqNr, PseudoApp);
                            execute.Active = true;
                            serv.ProtocolActive = true;
                            serv.GarbageViewChangeRegistry(serv.CurView);
                            serv.ResetClientStatus();
                            await Sync.Next();
                        }
                    }
                }
            }
        }
        
        public static async CTask<bool> ListenForShutdown(Source<bool> shutdown)
        {
            Console.WriteLine("ListenForShutdown");
            return await shutdown.Next();
        }
        
        public static async CTask<bool> AppOperation(Request req, Server serv, ProtocolExecution execute, int curSeq, CancellationTokenSource cancel)
        {
            var reply = await execute.HandleRequest(req, curSeq, cancel);
            if (reply.Status && execute.Active) PseudoApp.Add(reply.Result);
            Console.WriteLine("AppCount:" + PseudoApp.Count);
            return true;
        }
        
        //public static async Task ViewChangeOperation(ProtocolExecution execute) => await execute.HandlePrimaryChange();
    }
}