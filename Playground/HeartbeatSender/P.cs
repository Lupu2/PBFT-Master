using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.StorageEngine.SqlServer;

namespace Playground.HeartbeatSender
{
    internal static class P
    {
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
            DatabaseHelper.CreateDatabaseIfNotExist("localhost", "Playground");

            var sqlStorageEngine = new SqlServerStorageEngine(
                "HeartbeatSender",
                DatabaseHelper.ConnectionString("localhost", "Playground")
            );
            sqlStorageEngine.Initialize();
            sqlStorageEngine.Clear();

            var engine = ExecutionEngineFactory.StartNew(sqlStorageEngine);
            engine.Schedule(() => new HeartbeatSender().Start());

            return engine;
        }

        private static Engine Continue()
        {
            var sqlStorageEngine = new SqlServerStorageEngine(
                "HeartbeatSender",
                DatabaseHelper.ConnectionString("localhost", "Playground")
            );

            return ExecutionEngineFactory.Continue(sqlStorageEngine);
        }
    }
}
