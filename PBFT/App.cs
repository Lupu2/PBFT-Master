using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
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
                var test = LoadJSONValues.GetServerData("serverInfo.json", int.Parse(args[0])).Result;
                var id = test.Item1;
                var ipaddr = test.Item2;
                Console.WriteLine("Result");
                Console.WriteLine(id);
                Console.WriteLine(ipaddr);
                var serversInfo = LoadJSONValues.LoadJSONFileContent("serverInfo.json").Result;
                Console.WriteLine(serversInfo[0]);
                var con = File.Exists("./PBFTStorage.txt");
                Engine scheduler;
                Server server;
                Source<Request> reqSource = new Source<Request>();
                Source<PhaseMessage> protSource = new Source<PhaseMessage>();
                if (!con)
                {
                    scheduler = ExecutionEngineFactory.StartNew(storageEngine);
                    server = new Server(id, 0, serversInfo.Count, scheduler, 15, ipaddr, reqSource,
                        protSource,serversInfo); //int id, int curview, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge
                }
                else
                {
                    scheduler = ExecutionEngineFactory.Continue(storageEngine);
                    server = new Server(id, 0, serversInfo.Count, scheduler, 15, ipaddr, reqSource,
                        protSource, serversInfo); //TODO update with that collected in the storageEngine
                    //load server data
                }
                server.Start();
                _ = server.InitializeConnections();
                //Server serv = new Server(id, 0, scheduler, 10);
                //HandleRequest(serv, protexec, reqSource, protSource)
                ProtocolExecution protexec = new ProtocolExecution(server, 1, protSource);
                _ = RequestHandler(server, protexec, reqSource, scheduler);
            }
        }

        public static async Task RequestHandler(Server serv, ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            while (true)
            {
                var req = await requestMessage.Next();
                if (Crypto.VerifySignature(req.Signature, req.CreateCopyTemplate().SerializeToBuffer(), serv.ClientPubKeyRegister[req.ClientID]))
                {
                    Console.WriteLine("Handling client request");
                    _ = scheduler.Schedule(() =>
                    {
                        serv.ChangeClientStatus(req.ClientID);
                        var reply = execute.HandleRequest(req);
                        Console.WriteLine(reply);
                        serv.ChangeClientStatus(req.ClientID);
                    });    
                }
                
            }
        }
    }
}