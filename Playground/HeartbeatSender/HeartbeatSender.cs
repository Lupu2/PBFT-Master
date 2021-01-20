using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.HeartbeatSender
{
    internal class HeartbeatSender : IPersistable
    {
        private int j = Int32.MinValue;
        public async CTask Start()
        {
            var i = Int32.MinValue;
            
            while (true)
            {
                Console.WriteLine($"Heartbeat: {i++}");
                await Sleep.Until(1000);
            }
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set("j", j);
        }

        private static HeartbeatSender Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new HeartbeatSender() {j = sd.Get<int>("j")};
        }
    }
}
