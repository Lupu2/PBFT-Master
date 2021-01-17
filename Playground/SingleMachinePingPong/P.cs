using System;
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
            var storage = CreateStorageEngine(storageEngine, true);
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

            Console.WriteLine("PRESS ENTER TO START PING PONG APP");
            Console.ReadLine();

            Continue(storageEngine);
        }

        public static void Continue(StorageEngineImplementation storageEngine)
        {
            var storage = CreateStorageEngine(storageEngine, false);
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

        private static readonly InMemoryStorageEngine InMemoryStorageEngine = new();
        private static IStorageEngine CreateStorageEngine(StorageEngineImplementation storageEngine, bool initialize)
        {
            switch (storageEngine)
            {
                case StorageEngineImplementation.SqlServer:
                    DatabaseHelper.CreateDatabaseIfNotExist("localhost", "ping_pong", "sa", "Pa55word");
                    var storage = new SqlServerStorageEngine("1", DatabaseHelper.ConnectionString("localhost", "ping_pong", "sa", "Pa55word"));
                    if (initialize)
                    {
                        storage.Initialize();
                        storage.Clear();
                    }
                    return storage;
                case StorageEngineImplementation.File:
                    return new SimpleFileStorageEngine(@"./PingPong.txt", initialize);
                case StorageEngineImplementation.InMemory:
                    return InMemoryStorageEngine;
                default:
                    throw new ArgumentOutOfRangeException(nameof(storageEngine), storageEngine, null);
            }
        }
    }
}
