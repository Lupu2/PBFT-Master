using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
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
            
            var test = LoadJSONValues.GetServerData("serverInfo.json", 0).Result;
            var id = test.Item1;
            var ipaddr = test.Item2;
            Console.WriteLine("Result");
            Console.WriteLine(id);
            Console.WriteLine(ipaddr);
            var serversInfo = LoadJSONValues.LoadJSONFileContent("serverInfo.json").Result;

            if (args.Length > 0) //add arguments by editing configuration program arguments or by adding parameters behind executable directly
            {
                foreach (var arg in args)
                {
                    Console.WriteLine(arg);
                }

                var con = File.Exists("./PBFTStorage.txt");
                Engine scheduler;
                Server server;
                Source<Request> reqSource = new Source<Request>();
                Source<PhaseMessage> protSource = new Source<PhaseMessage>();
                if (!con)
                {
                    scheduler = ExecutionEngineFactory.StartNew(storageEngine);
                    server = new Server( Int32.Parse(args[0]), 0, scheduler, 15, args[1], reqSource,
                        protSource); //int id, int curview, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge
                }
                else
                {
                    scheduler = ExecutionEngineFactory.Continue(storageEngine);
                    server = new Server(Int32.Parse(args[0]), 0, scheduler, 15, args[1], reqSource,
                        protSource); //TODO update with that collected in the storageEngine
                    //load server data
                }
                server.Start();
                //Server serv = new Server(id, 0, scheduler, 10);
                //HandleRequest(serv, protexec, reqSource, protSource)
                ProtocolExecution protexec = new ProtocolExecution(server, 1, protSource);
                _ = HandleRequest(server, protexec, reqSource, scheduler);
            }
        }

        public static async CTask HandleRequest(Server serv, ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            while (true)
            {
                var req = await requestMessage.Next();
                await scheduler.Schedule(() =>
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