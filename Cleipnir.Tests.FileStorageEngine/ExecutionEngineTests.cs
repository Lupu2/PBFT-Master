using System;
using System.Collections.Generic;
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cleipnir.Tests.FileStorageEngine
{
    [TestClass]
    public class ExecutionEngineTests
    {
        [TestMethod]
        public void Test()
        {
            var storageEngine = new SimpleFileStorageEngine(nameof(ExecutionEngineTests), true);
            var executionEngine = ExecutionEngineFactory.StartNew(storageEngine);

            executionEngine.Schedule(() => new DelayAndDo().SayHallo());

            Thread.Sleep(5000);
            executionEngine.Dispose();

            Console.WriteLine("AFTER DISPOSE");
            Thread.Sleep(1000);
            executionEngine = ExecutionEngineFactory.Continue(storageEngine);
            Thread.Sleep(100000);
        }

        private class DelayAndDo : IPersistable
        {
            public async CTask SayHallo()
            {
                while (true)
                {
                    await Sleep.Until(1000);
                    Console.WriteLine("Hello");    
                }
            }
            
            public void Serialize(StateMap sd, SerializationHelper helper) { }

            private static DelayAndDo Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new DelayAndDo();
            }
        }        
    }
}