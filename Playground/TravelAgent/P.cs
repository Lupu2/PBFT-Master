using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground.TravelAgent
{
    public static class P
    {
        public static void Do() => StartNew();
        
        public static void StartNew()
        {
            var storage = new SimpleFileStorageEngine(@"./TravelAgent.txt", true);
            var scheduler = ExecutionEngineFactory.StartNew(storage);

            scheduler.Schedule(() =>
            {
                var workflow = new Workflow();
                _ = workflow.Do();
            });

            Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
            Console.ReadLine();

            scheduler.Dispose();

            Console.WriteLine("PRESS ENTER TO START PING PONG APP");
            Console.ReadLine();

            Continue();
        }

        public static void Continue()
        {
            var storage = new SimpleFileStorageEngine(@"./TravelAgent.txt", false);
            var scheduler = ExecutionEngineFactory.Continue(storage);

            while (true)
            {
                Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
                Console.ReadLine();

                scheduler.Dispose();

                Console.WriteLine("PRESS ENTER TO START PING PONG APP");
                Console.ReadLine();

                scheduler = ExecutionEngineFactory.Continue(storage);
            }
        }
    }
}