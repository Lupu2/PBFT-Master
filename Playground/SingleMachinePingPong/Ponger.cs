using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;

namespace Playground.SingleMachinePingPong
{
    class Ponger : IPersistable
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

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(Messages), Messages);
            sd.Set(nameof(Count), Count);
        }

        private static Ponger Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new Ponger()
            {
                Count = (int) sd[nameof(Count)],
                Messages = (Source<string>) sd[nameof(Messages)]
            };
        }
    }
}