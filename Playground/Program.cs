using System;
using System.Collections.Generic;
using System.Text;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.NetworkCommunication;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;
using static Playground.SingleMachinePingPong.StorageEngineImplementation;

namespace Playground
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            SingleMachinePingPong.P.StartNew(File);
            
            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
