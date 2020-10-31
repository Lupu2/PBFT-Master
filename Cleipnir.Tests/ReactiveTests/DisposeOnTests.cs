using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class DisposeOnTests
    {
        [TestMethod]
        public void SubscriptionIsDisposedAfterAwaitableCompletes()
        {
            var storage = new InMemoryStorageEngine();
            var os = new ObjectStore(storage);

            var source = new Source<int>();
            var awaitable = new CAwaitable();
            var valueHolder = new ValueHolder<int>();

            source.DisposeOn(awaitable).CallOnEvent(valueHolder.SetValue);

            source.Emit(1);
            valueHolder.Value.ShouldBe(1);

            os.Attach(source);
            os.Attach(awaitable);
            os.Attach(valueHolder);

            os.Persist();

            os = ObjectStore.Load(storage, true);
            source = os.Resolve<Source<int>>();
            awaitable = os.Resolve<CAwaitable>();
            valueHolder = os.Resolve<ValueHolder<int>>();

            source.Emit(2);
            valueHolder.Value.ShouldBe(2);

            awaitable.SignalCompletion();

            source.Emit(3);
            valueHolder.Value.ShouldBe(2);

            os.Persist();

            os = ObjectStore.Load(storage, true);
            source = os.Resolve<Source<int>>();
            awaitable = os.Resolve<CAwaitable>();
            valueHolder = os.Resolve<ValueHolder<int>>();

            source.Emit(4);
            valueHolder.Value.ShouldBe(2);
        }
    }
}
