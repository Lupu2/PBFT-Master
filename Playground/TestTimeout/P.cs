using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.Rx.ExecutionEngine;
using Cleipnir.StorageEngine.InMemory;
using Org.BouncyCastle.Asn1.Cms;

namespace Playground.TestTimeout
{
    public static class P
    {
        public static void Do()
        {
            var storageEngine = new InMemoryStorageEngine();
            var executionEngine = ExecutionEngineFactory.StartNew(storageEngine);
            executionEngine.Schedule(() =>
            {
                var shutdownsource = new Source<bool>();
                var cancel = new CancellationTokenSource();
                _ = TimeoutFunc(5000, shutdownsource, cancel.Token);
                Console.WriteLine("Waiting for result");
                var listen = ListenForMessage(shutdownsource).GetAwaiter();
                Thread.Sleep(1000);
                cancel.Cancel();
                Console.WriteLine("Cancelled!");
                bool res = listen.GetResult();
                Console.WriteLine("Result: " + res);
            });
        }

        public static async Task<bool> ListenForMessage(Source<bool> mesbridge)
        {
            var res = await mesbridge.Next();
            Console.WriteLine("Got result");
            return res;
        }
        
        public static async CTask TimeoutFunc(int length, Source<bool> messageReceiver, CancellationToken cancel)
        {
            try
            {
                await Task.Delay(length, cancel);
                messageReceiver.Emit(true);
            }
            catch (TaskCanceledException te)
            {
                Console.WriteLine("Timeout cancelled!");
            }
            
            
            
        }
    }
}
