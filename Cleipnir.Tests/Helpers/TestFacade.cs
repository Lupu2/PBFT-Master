using System;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.StorageEngine.InMemory;

namespace Cleipnir.Tests.Helpers
{
    internal class TestFacade : IDisposable
    {
        public InMemoryStorageEngine StableStorageEngine { get; }
        public Engine Scheduler { get; private set; }

        public TestFacade()
        {
            StableStorageEngine = new InMemoryStorageEngine();
            Scheduler = ExecutionEngine.ExecutionEngineFactory.StartNew(StableStorageEngine);
        }

        public void LoadAgain()
        {
            Scheduler.Dispose();
            
            Scheduler = ExecutionEngine.ExecutionEngineFactory.Continue(StableStorageEngine);
        } 

        public void PersistAndCloseDown() => Scheduler.Dispose();

        public Task Schedule(Action a) => Scheduler.Schedule(a);
        public Task<T> Schedule<T>(Func<T> f) => Scheduler.Schedule(f);

        public T Resolve<T>() => Scheduler.Resolve<T>().Result;

        public void Dispose() => Scheduler.Dispose();
    }
}
