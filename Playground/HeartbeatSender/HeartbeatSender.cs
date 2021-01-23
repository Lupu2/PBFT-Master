using System;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.HeartbeatSender
{
    internal class HeartbeatSender : IPropertyPersistable
    {
        public async CTask Start()
        {
            var i = 0;
            
            while (true)
            {
                Console.WriteLine($"Heartbeat: {i++}");
                await Sleep.Until(1000);
            }
        }
    }
}
