using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.StorageEngine.SimpleFile;
using Cleipnir.StorageEngine.SqlServer;

namespace Playground.HeartbeatSender
{
    internal static class P
    {
        private static readonly InMemoryStorageEngine StorageEngine = new InMemoryStorageEngine();
        private static readonly SimpleFileStorageEngine FileStorageEngine = new SimpleFileStorageEngine("test", true);

        private static readonly SqlServerStorageEngine SqlServerStorageEngine =
            new SqlServerStorageEngine("instance1", DatabaseHelper.ConnectionString("localhost", "test2"));
        
        public static void Do()
        {
            var engine = Start();

            while (true)
            {
                Console.WriteLine("Press enter to stop");
                Console.ReadLine();

                engine.Dispose();

                Console.WriteLine("Press enter to start");
                Console.ReadLine();

                engine = Continue();
            }
        }

        private static Engine Start()
        {
            SqlServerStorageEngine.Initialize();
            SqlServerStorageEngine.Clear();
            //var storageEngine = new SimpleFileStorageEngine("test", true);
           // var storageEngine = new InMemoryStorageEngine();
            var engine = ExecutionEngineFactory.StartNew(FileStorageEngine);
            engine.Schedule(() => new HeartbeatSender().Start());

            return engine;
        }

        private static Engine Continue()
        {
            //var storageEngine = new SimpleFileStorageEngine("test", false);

            return ExecutionEngineFactory.Continue(FileStorageEngine);
        }
    }
}
