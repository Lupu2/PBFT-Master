using System;
using System.Collections.Generic;
using System.IO;
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
            
            /*if (args.Length > 0) //add arguments by editing configuration program arguments or by adding parameters behind executable directly
            {
                foreach (var arg in args)
                {
                    Console.WriteLine(arg);
                }
            }
            var con = File.Exists("./PBFTStorage.txt");
            Engine scheduler;
            if (!con)
            {
                scheduler = ExecutionEngineFactory.StartNew(storageEngine);    
            }
            else
            {
                scheduler = ExecutionEngineFactory.Continue(storageEngine);
            }
            //Server serv = new Server(id, 0, scheduler, 10);
            Source<Request> reqSource = new Source<Request>();
            Source<PhaseMessage> protSource = new Source<PhaseMessage>();*/
            
            //HandleRequest(serv, protexec, reqSource, protSource)
        }

        public static async CTask HandleRequest(Server serv, ProtocolExecution execute, Source<Request> requestMessage, Engine scheduler)
        {
            while (true)
            {
                var req = await requestMessage.Next();
                await scheduler.Schedule(() =>
                {
                    var reply = execute.HandleRequest(req);
                    Console.WriteLine(reply);
                });
            }
        }
    }
}