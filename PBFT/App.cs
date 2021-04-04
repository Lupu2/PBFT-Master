using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
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
        public static void Run(string[] args)
        {
            Console.WriteLine("Application running...");
            Console.WriteLine(args.Length);
            
            if (args.Length > 0) //add arguments by editing configuration program arguments or by adding parameters behind executable directly
            {
                Console.WriteLine("arguments:");
                foreach (var arg in args) Console.WriteLine(arg);
                
                //Format id=id test=true/false
                Console.WriteLine(args[0].Split("id=")[1]);
                int paramid = Int32.Parse(args[0].Split("id=")[1]);
                bool testparam = Boolean.Parse(args[1].Split("test=")[1]);
                var storageEngine = new SimpleFileStorageEngine(".PBFTStorage"+paramid+".txt", true); //change to false when done debugging
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
                var con = File.Exists("./PBFTStorage.txt");
                Engine scheduler;
                Server server = null;
                Source<Request> reqSource = new Source<Request>();
                Source<PhaseMessage> protSource = new Source<PhaseMessage>();
                Source<bool> viewSource = new Source<bool>();
                Source<ViewChangeCertificate> shutdownSource = new Source<ViewChangeCertificate>();
                Source<NewView> newviewSource = new Source<NewView>();
                Source<CheckpointCertificate> checkSource = new Source<CheckpointCertificate>();
                SourceHandler sh = new (reqSource, protSource, viewSource, shutdownSource, newviewSource, checkSource);
                PseudoApp = new CList<string>();
                if (!con)
                {
                    scheduler = ExecutionEngineFactory.StartNew(storageEngine);
                    
                    //server = new Server(id, 0, serversInfo.Count, scheduler, 20, ipaddr, reqSource, protSource, viewSource, shutdownSource, newviewSource ,serversInfo);
                    server = new Server(id, 0, serversInfo.Count, scheduler, 5, ipaddr, sh, serversInfo);
                    scheduler.Schedule(() =>
                    {
                        Roots.Entangle(PseudoApp);
                        Roots.Entangle(server);
                    });

                    //protSource,serversInfo); //int id, int curview, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge
                }
                else
                {
                    //load persistent data
                    scheduler = ExecutionEngineFactory.Continue(storageEngine);
                    //server = new Server(id, 0, serversInfo.Count, scheduler, 15, ipaddr, reqSource,
                    //   protSource, viewSource, shutdownSource, serversInfo); //TODO update with that collected in the storageEngine
                    scheduler.Schedule(() =>
                    {
                        server = Roots.Resolve<Server>();
                        PseudoApp = Roots.Resolve<CList<string>>();
                    });
                    server.AddEngine(scheduler);


                }
                server.Start();
                Thread.Sleep(1000);
                ProtocolExecution protexec = new ProtocolExecution(server, 1, protSource, viewSource, newviewSource);
                server.InitializeConnections()
                    .GetAwaiter()
                    .OnCompleted(() => StartRequestHandler(protexec, reqSource, scheduler));
                //Server serv = new Server(id, 0, scheduler, 10);
                //HandleRequest(serv, protexec, reqSource, protSource)
                
                //_ = RequestHandler(server, protexec, reqSource, scheduler);
                Console.ReadLine();
                server.Dispose();
            }
        }

        public static void StartRequestHandler(ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            _ = RequestHandler(execute, requestMessage, scheduler);
        }
        
        public static async CTask RequestHandler(ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            Server serv = execute.Serv;
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
                        //await scheduler.Schedule(() => execute.HandleRequest(req));
                        //serv.ChangeClientStatus(req.ClientID);
                        CancellationTokenSource cancel = new CancellationTokenSource();
                        _ = TimeoutOps.AbortableProtocolTimeoutOperation(serv.Subjects.ShutdownSubject, 10000,
                            cancel.Token);
                        //await Task.WhenAny(scheduler.Schedule(() =>
                        var a = await Task.WhenAny(scheduler.Schedule<int>(() =>
                            {
                                int seq = ++serv.CurSeqNr;
                                execute.Serv.ChangeClientStatus(req.ClientID);
                                //var timeout = TimeoutOps.;
                                var operation = AppOperation(req, serv, execute, seq, cancel).GetAwaiter();
                                operation.OnCompleted(() =>
                                {
                                    execute.Serv.ChangeClientStatus(req.ClientID);
                                    if (seq % serv.CheckpointConstant == 0 && serv.CurSeqNr != 0
                                    ) //really shouldn't call this at seq nr 0, but just incase
                                        serv.CreateCheckpoint(execute.Serv.CurSeqNr, PseudoApp);
                                    Console.WriteLine("FINISHED TASK");
                                });
                                return 0;
                            })
                        );
                        //does not work fsr, assume its because of the scheduler not liking GetAwaiter().GetResult()
                        /*int a = await scheduler.Schedule<int>(() =>
                        {
                            int seq = ++serv.CurSeqNr;
                            execute.Serv.ChangeClientStatus(req.ClientID);
                            //var timeout = TimeoutOps.;
                            var operation = AppOperation(req, serv, execute, seq, cancel).GetAwaiter();
                            operation.OnCompleted(() =>
                            {
                                execute.Serv.ChangeClientStatus(req.ClientID);
                                if (seq % serv.CheckpointConstant == 0 && serv.CurSeqNr != 0
                                ) //really shouldn't call this at seq nr 0, but just incase
                                    serv.CreateCheckpoint(execute.Serv.CurSeqNr, PseudoApp);
                                Console.WriteLine("FINISHED TASK");
                            });
                            return 0;
                        });*/

                        //return 0;
                        //})//, ListenForShutdown(serv.Subjects.ShutdownSubject))


                        /*if (res == 1)
                        {
                            await scheduler.Schedule(() =>
                            {
                                serv.ResetClientStatus();
                                execute.Active = false;
                                var vcop = ViewChangeOperation(execute).GetAwaiter();
                                vcop.OnCompleted(() =>
                                {
                                    execute.Active = true;
                                    Console.WriteLine("ViewChange Completed!");
                                });
                            });
                        }*/


                    }
                }
            }
        }

        /*public static void CreateCheckpoint(Engine eng, Server serv)
        {
            eng.Schedule(() =>
            {
                serv.CreateCheckpoint(serv.CurSeqNr, PseudoApp);
            });
        }*/

        public static async Task<int> ListenForShutdown(Source<ViewChangeCertificate> shutdown)
        {
            Console.WriteLine("Shutting down");
            await shutdown.Next();
            return 1;
        }
        
        public static async CTask AppOperation(Request req, Server serv, ProtocolExecution execute, int curSeq, CancellationTokenSource cancel)
        {
            var reply = await execute.HandleRequest(req, curSeq, cancel);
            PseudoApp.Add(reply.Result);
            Console.WriteLine("AppCount:" + PseudoApp.Count);
        }

        public static async CTask ViewChangeOperation(ProtocolExecution execute)
        {
            await execute.HandlePrimaryChange();
        }
    }
}