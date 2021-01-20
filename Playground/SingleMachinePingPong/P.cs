using System;
using System.Linq;
using Cleipnir.ExecutionEngine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.StorageEngine.SimpleFile;
using Cleipnir.StorageEngine.SqlServer;

namespace Playground.SingleMachinePingPong
{
    internal static class P
    {
        public static void StartNew(StorageEngineImplementation storageEngine)
        {
            var storage = new SimpleFileStorageEngine("./ping_pong.txt", true);
            /*DatabaseHelper.CreateDatabaseIfNotExist("localhost", "ping_pong", "sa", "Pa55word");
            var storage = new SqlServerStorageEngine("1", DatabaseHelper.ConnectionString("localhost", "ping_pong", "sa", "Pa55word"));
          
                storage.Initialize();
                storage.Clear();*/
                var scheduler = ExecutionEngineFactory.StartNew(storage);

            scheduler.Schedule(() =>
            {
                var messages = new Source<string>();
                var pinger = new Pinger { Messages = messages };
                var ponger = new Ponger { Messages = messages };

                _ = ponger.Start();
                _ = pinger.Start();
            });

            Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
            Console.ReadLine();

            scheduler.Dispose();
            storage.Dispose();

            Console.WriteLine("PRESS ENTER TO START PING PONG APP");
            Console.ReadLine();

            Continue();
        }

        private static void Continue()
        {
            var storage = new SimpleFileStorageEngine("./ping_pong.txt", false);
            //var storage = new SqlServerStorageEngine("1", DatabaseHelper.ConnectionString("localhost", "ping_pong", "sa", "Pa55word"));

            var engine = ExecutionEngineFactory.Continue(storage);

            while (true)
            {
                Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
                Console.ReadLine();

                engine.Dispose();

                Console.WriteLine("PRESS ENTER TO START PING PONG APP");
                Console.ReadLine();

                engine = ExecutionEngineFactory.Continue(storage);
            }
        }
    }
}
