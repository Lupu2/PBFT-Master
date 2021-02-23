using System;
<<<<<<< HEAD
using System.Collections.Generic;
using System.Net;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.SimpleFile;
=======
>>>>>>> a1b03f82acdea7a83d4db277f9dc09338df26149

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
            PersonExample.P.Do();
            
            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
