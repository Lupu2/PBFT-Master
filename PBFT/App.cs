using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT
{
    public static class App
    {
        public static void Run(string[] args)
        {
            Console.WriteLine("Application running...");
            var storageEngine = new SimpleFileStorageEngine(".PBFTStorage.txt", true); //change to false when done debugging
            Console.WriteLine(args.Length);
            
            if (args.Length > 0) //add arguments by editing configuration program arguments or by adding parameters behind executable directly
            {
                Console.WriteLine("arguments:");
                foreach (var arg in args) Console.WriteLine(arg);
                
                //Format id=id test=true/false
                Console.WriteLine(args[0].Split("id=")[1]);
                int paramid = Int32.Parse(args[0].Split("id=")[1]);
                bool testparam = Boolean.Parse(args[1].Split("test=")[1]);
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
                Server server;
                Source<Request> reqSource = new Source<Request>();
                Source<PhaseMessage> protSource = new Source<PhaseMessage>();
                if (!con)
                {
                    scheduler = ExecutionEngineFactory.StartNew(storageEngine);
                    server = new Server(id, 0, serversInfo.Count, scheduler, 20, ipaddr, reqSource, protSource,
                        serversInfo);
                    //protSource,serversInfo); //int id, int curview, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge
                }
                else
                {
                    //load persistent data
                    scheduler = ExecutionEngineFactory.Continue(storageEngine);
                    server = new Server(id, 0, serversInfo.Count, scheduler, 15, ipaddr, reqSource,
                        protSource, serversInfo); //TODO update with that collected in the storageEngine
                    
                }
                server.Start();
                Thread.Sleep(1000);
                ProtocolExecution protexec = new ProtocolExecution(server, 1, protSource);
                server.InitializeConnections()
                    .GetAwaiter()
                    .OnCompleted(() => StartRequestHandler(server, protexec, reqSource, scheduler));
                //Server serv = new Server(id, 0, scheduler, 10);
                //HandleRequest(serv, protexec, reqSource, protSource)
                
                //_ = RequestHandler(server, protexec, reqSource, scheduler);
                Console.ReadLine();
            }
        }

        public static void StartRequestHandler(Server serv, ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            _ = RequestHandler(serv, execute, requestMessage, scheduler);
        }
        
        public static async Task RequestHandler(Server serv, ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            while (true)
            {
                var req = await requestMessage.Next();
                if (Crypto.VerifySignature(req.Signature, req.CreateCopyTemplate().SerializeToBuffer(), serv.ClientPubKeyRegister[req.ClientID]))
                {
                    Console.WriteLine("Handling client request");
                    //await scheduler.Schedule(() => execute.HandleRequest(req));
                    //serv.ChangeClientStatus(req.ClientID);
                    await scheduler.Schedule(() =>
                    {
                        serv.ChangeClientStatus(req.ClientID);
                        var reply = execute.HandleRequest(req)
                            .GetAwaiter();
                        reply.OnCompleted(() =>
                        {
                            serv.ChangeClientStatus(req.ClientID);
                            Console.WriteLine("It worked!");
                        });
                    });
                    //serv.ChangeClientStatus(req.ClientID);
                }
                
            }
        }
    }
}