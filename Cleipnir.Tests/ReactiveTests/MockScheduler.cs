using System;
using Cleipnir.ExecutionEngine;

namespace Cleipnir.Tests.ReactiveTests
{
    internal class MockScheduler : IScheduler
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Schedule(Action toExecute, bool persistent)
        {
            throw new NotImplementedException();
        }

        public void FireAndForget(Action toExecute)
        {
            throw new NotImplementedException();
        }
    }
}
