using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground.SingleMachinePingPong
{
    internal static class P
    {
        public static void StartNew()
        {
            var storage = new SimpleFileStorageEngine(@"./PingPong.txt", true);
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

            Continue();
        }

        public static void Continue()
        {
            var storage = new SimpleFileStorageEngine(@".\PingPong.txt", false);
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

    class Pinger : IPropertyPersistable
    {
        public Source<string> Messages { get; set; }
        public int Count { get; set; }

        public async CTask Start()
        {
            while (true)
            {
                await Sleep.Until(1000);
                Messages.Emit($"PING {Count++}");
                var msg = await Messages.Where(m => m.StartsWith("PONG")).Next();
                Console.WriteLine("PINGER: " + msg);
            }
        }
    }

    class Ponger : IPropertyPersistable
    {
        public Source<string> Messages { get; set; }
        public int Count { get; set; }

        public async CTask Start()
        {
            while (true)
            {
                var msg = await Messages.Where(s => s.StartsWith("PING")).Next();
                Console.WriteLine($"PONGER: {msg}" );
                await Sleep.Until(1000);
                Messages.Emit($"PONG {Count++}");
            }
        }
    }
}
