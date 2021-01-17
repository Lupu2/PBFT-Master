using System;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;

namespace Playground.SingleMachinePingPong
{
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
}