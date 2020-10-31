using System.Linq;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.GarbageCollectionTests
{
    [TestClass]
    public class SchedulerGcTests
    {
        private InMemoryStorageEngine StableStorageEngine { get; set; }
        private Engine Scheduler { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            StableStorageEngine = new InMemoryStorageEngine();
            Scheduler = ExecutionEngine.ExecutionEngineFactory.StartNew(StableStorageEngine);
        }

        [TestCleanup]
        public void CleanUp() => Scheduler.Dispose();

        [TestMethod]
        public async Task GarbageCollectableInstancesAreRemovedFromStableStorage()
        {
            var p3 = new Person {Name = "P3"};
            var p2 = new Person {Name = "P2", Other = p3};
            var p1 = new Person {Name = "P1", Other = p2};

            await Scheduler.Entangle(p1);
            await Scheduler.Sync();

            var count = StableStorageEngine.Load().Count(e => e.Value is string s && (s == "P3" || s == "P2" || s == "P1"));
            count.ShouldBe(3);

            await Scheduler.Schedule(() => p1.Other = null);
            await Scheduler.Sync();
            Scheduler.Dispose();

            count = StableStorageEngine.Load().Count(e => e.Value is string s && (s == "P3" || s == "P2" || s == "P1"));
            count.ShouldBe(1);
        }
    }
}
