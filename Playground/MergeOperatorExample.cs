using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;

namespace Playground
{
    public static class MergeOperatorExample
    {
        public static void Do()
        {
            var storage = new InMemoryStorageEngine();
            var engine = ExecutionEngineFactory.StartNew(storage);

            engine.Schedule(() =>
            {
                var workflow = new Workflow() {
                    Source1 = new Source<string>(), 
                    Source2 = new Source<string>()
                };
                _ = workflow.Execute();
                Roots.Entangle(workflow);
            }).Wait();
            
            engine.Sync().Wait();
            engine.Dispose();

            engine = ExecutionEngineFactory.Continue(storage);

            engine.Schedule(() =>
            {
                var workflow = Roots.Resolve<Workflow>();
                workflow.Source1.Emit("SOURCE_1");
                //workflow.Source2.Emit("SOURCE_2");
            });
        }

        private class Workflow : IPersistable
        {
            public Source<string> Source1 { get; set; }
            public Source<string> Source2 { get; set; }

            public async CTask Execute()
            {
                Console.WriteLine("EXECUTION STARTED!");
                var firstEmitted = await Source1.Merge(Source2).Next();
                Console.WriteLine("FIRST EMITTED WAS: " + firstEmitted);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd[nameof(Source1)] = Source1;
                sd[nameof(Source2)] = Source2;
            }

            private static Workflow Deserialize(IReadOnlyDictionary<string, object> sd)
                => new Workflow()
                {
                    Source1 = sd.Get<Source<string>>(nameof(Source1)),
                    Source2 = sd.Get<Source<string>>(nameof(Source2))
                };
        }
    }
}