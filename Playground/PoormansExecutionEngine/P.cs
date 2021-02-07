using System;
using System.IO;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground.PoormansExecutionEngine
{
    public static class P
    {
        public static void Do()
        {
            var @continue = File.Exists(@"./timer.txt");
            if (@continue)
            {
                Continue();
                return;
            }
            
            var storageEngine = new SimpleFileStorageEngine(@"./timer.txt", false);
            var scheduler = new PoormansExecutionEngine();
            scheduler.Start(storageEngine, false);

            scheduler.Schedule(() => Console.WriteLine("Hello From Anonymous Function"));

            var cw = new ConsoleWriter("Hello From Console Writer");
            new PersistentTimer(
                DateTime.Now + TimeSpan.FromSeconds(10),
                cw.Do,
                false,
                scheduler
            ).Start();
        }

        private static void Continue()
        {
            Console.WriteLine("CONTINUING!");
            
            var storageEngine = new SimpleFileStorageEngine($"./timer.txt", false);
            
            var scheduler = new PoormansExecutionEngine();
            scheduler.Start(storageEngine, true);
        }
    }
}