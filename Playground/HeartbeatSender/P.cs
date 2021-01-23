using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.StorageEngine;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.StorageEngine.SimpleFile;
using Cleipnir.StorageEngine.SqlServer;

namespace Playground.HeartbeatSender
{
    internal static class P
    {
        public static void Do()
        {
            var storageEngine = new InMemoryStorageEngine();
            var engine = Start(storageEngine);

            while (true)
            {
                Console.WriteLine("Press enter to stop");
                Console.ReadLine();

                engine.Dispose();

                Console.WriteLine("Press enter to start");
                Console.ReadLine();

                engine = Continue(storageEngine);
            }
        }

        private static Engine Start(IStorageEngine storageEngine)
        {
            var engine = ExecutionEngineFactory.StartNew(storageEngine);
            engine.Schedule(() => new HeartbeatSender().Start());

            return engine;
        }

        private static Engine Continue(IStorageEngine storageEngine) 
            => ExecutionEngineFactory.Continue(storageEngine);
    }
}
