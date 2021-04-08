using System;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;

namespace Playground
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            //ReactiveFun.P.Do();
            //SocketIPExample.IPHostListExample.GetIpAddressList(Dns.GetHostName());
            //SocketIPExample.asyncserversockettut.P();
            //SimpleNetwork.P.Do();
            //PersonExample.P.Do();
            //TestTimeout.P.Do();
            var engine = ExecutionEngineFactory.StartNew(new InMemoryStorageEngine());
            engine.Schedule(() => _ = WhenAnyTest());

            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }

        private static async CTask WhenAnyTest()
        {
            var t1 = Do1();
            var t2 = Do2();
            var res = await WhenAny<int>.Of(t1, t2);
            Console.WriteLine("WhenAnyTest got result: " + res);
        }

        private static async CTask<int> Do1()
        {
            await Sleep.Until(1000);
            Console.WriteLine("Do1 completed");
            return 1;
        }

        private static async CTask<int> Do2()
        {
            await Sleep.Until(2000);
            Console.WriteLine("Do2 completed");
            return 2;
        }
    }
}
