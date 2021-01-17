using System;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;

namespace Playground.SingleMachinePingPong
{
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