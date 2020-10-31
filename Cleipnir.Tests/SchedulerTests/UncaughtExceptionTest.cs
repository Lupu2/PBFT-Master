using System;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.SchedulerTests
{
    [TestClass]
    public class UncaughtExceptionTest
    {
        [TestMethod]
        public void SchedulerContinuesDespiteExecutingMethodDoesNotCatchingThrownException()
        {
            var storage = new InMemoryStorageEngine();
            var scheduler = ExecutionEngine.ExecutionEngineFactory.StartNew(storage);

            scheduler.Schedule(() => throw new NotImplementedException());

            var task = scheduler.Schedule(() => 1);

            task.Result.ShouldBe(1);
            
            scheduler.Dispose();
        }
    }
}
