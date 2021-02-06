using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;

namespace Playground.SimulateSendReceive
{
    public class Worker : IPersistable
    {
        public Source<Work> Contactmedium;
        public string name;

        public int income;
        public async CTask Start()
        {
            while(true)
            {
                Work work = await Contactmedium.Where(m => m.assignedworker == this && !m.finished).Next();
                Console.WriteLine($"Worker: {name} has received work!");
                await work.PerformWork(this);
                Console.WriteLine($"Worker: {name} has finished work!");
                Contactmedium.Emit(work);
            }
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(Contactmedium), Contactmedium);
            stateToSerialize.Set(nameof(name), name);
        }

        private static Worker Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new Worker()
            {
                Contactmedium = (Source<Work>) sd[nameof(Contactmedium)],
                name = (string) sd[nameof(name)]
            };
        }
    }
}