using System.Collections.Generic;
using System.Threading;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Rx;
using Cleipnir.Rx.ExecutionEngine;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class SchedulerOperatorTests
    {
        [TestMethod]
        public void CallbackAfterScheduleOperatorIsScheduledForExecution()
        {
            var storage = new InMemoryStorageEngine();
            var scheduler = ExecutionEngine.ExecutionEngineFactory.StartNew(storage);

            var source = new Source<int>();
            var pair = new PairValueHolder();

            source.Schedule().CallOnEvent(pair.SetValue1);
            source.CallOnEvent(pair.SetValue2);

            scheduler.Schedule(() => source.Emit(1)).Wait();
            Thread.Yield();
            var history = scheduler.Schedule(() => pair.History).Result;

            history.Count.ShouldBe(2);
            history[0].ShouldBe(3);
            history[1].ShouldBe(2);

            scheduler.Dispose();
        }

        private class PairValueHolder : IPersistable
        {
            public int Value1 { get; private set; }
            public int Value2 { get; private set; }

            public List<int> History { get; } = new List<int>();

            public void SetValue1(int value)
            {
                Value1 = value + 1;
                History.Add(Value1);
            }

            public void SetValue2(int value)
            {
                Value2 = value + 2;
                History.Add(Value2);
            } 
            
            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Value1), Value1);
                sd.Set(nameof(Value2), Value2);
            }

            private static PairValueHolder Deserialize(IReadOnlyDictionary<string, object> sd)
                => new PairValueHolder()
                {
                    Value1 = sd.Get<int>(nameof(Value1)),
                    Value2 = sd.Get<int>(nameof(Value2))
                };
        }
    }
}
