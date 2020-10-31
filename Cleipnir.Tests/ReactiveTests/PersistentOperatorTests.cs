using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class PersistentOperatorTests
    {
        [TestMethod]
        public void SimpleStreamTest()
        {
            var storage = new InMemoryStorageEngine();
            var source = new Source<int>();
            var valueHolder = new ValueHolder();
            source.CallOnEvent(valueHolder.SetValue);
            var store = new ObjectStore(storage);
            store.Attach(source);
            store.Attach(valueHolder);
            store.Persist();

            store = ObjectStore.Load(storage, true, (IScheduler) new MockScheduler());
            source = store.Resolve<Source<int>>();
            valueHolder = store.Resolve<ValueHolder>();
            source.Emit(123);
            valueHolder.Value.ShouldBe(123);
        }

        [TestMethod]
        public void StreamObserversAreDeserialized()
        {
            var storage = new InMemoryStorageEngine();
            var source = new Source<int>();
            var valueHolder = new ValueHolder();
            source.Select(_ => _).CallOnEvent(valueHolder.SetValue);
            var store = new ObjectStore(storage);
            store.Attach(source);
            store.Attach(valueHolder);
            store.Persist();

            store = ObjectStore.Load(storage, true, (IScheduler) new MockScheduler());
            source = store.Resolve<Source<int>>();
            valueHolder = store.Resolve<ValueHolder>();
            source.Emit(123);
            valueHolder.Value.ShouldBe(123);
        }

        [TestMethod]
        public void CreatedOperatorSurvivesRestart()
        {
            var storage = new InMemoryStorageEngine();
            var source = new Source<int>();
            var holder  = new ValueHolder();
            var emitter = new ValueEmitter(source.Scan(0, (akk, i) => akk + i), holder);
            var store = new ObjectStore(storage);

            source.Emit(1);
            source.Emit(2);

            holder.Value.ShouldBe(3);

            store.Attach(source);
            store.Attach(holder);

            store.Persist();

            store = ObjectStore.Load(storage, true, new MockScheduler());
            source = store.Resolve<Source<int>>();
            holder = store.Resolve<ValueHolder>();
            holder.Value.ShouldBe(3);

            source.Emit(1);
            holder.Value.ShouldBe(4);
        }
    }
}
