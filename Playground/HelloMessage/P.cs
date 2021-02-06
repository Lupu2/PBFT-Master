using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using Cleipnir.ExecutionEngine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using System;
using System.Reflection.Metadata.Ecma335;

namespace Playground.HelloMessage
{
    public static class P
    {
        public static void DO() => StartNew();

        public static void StartNew() {
            var storage = new SimpleFileStorageEngine("./Hello.txt", true); //initialize storage engine

            var scheduler = ExecutionEngineFactory.StartNew(storage); //initialize the executionengine assigning the storage

            scheduler.Schedule(() => 
            {
                var bridge = new Source<Message>();
                var hello = new Hello{ id = 1, 
                                       subject = bridge, 
                                       //GivenMessage = new Message("Hello!", "blue", subtype.Hello, 0, 1)
                                       GivenMessage = new Message("Hello!", "blue", 0, 0, 1)
                                     };
                var resp = new Hello{ id = 2, 
                                      subject = bridge,
                                      //GivenMessage = new Message("Hi There!","red",subtype.Response,1,0) 
                                      GivenMessage = new Message("Hi There!", "red",1, 1, 0) 
                                    };

                _ = hello.Start();
                _ = resp.Responder();

            });

            Console.WriteLine("Press 9 to stop app");
            bool app = true;
            while (app)
            {
                var key = Console.ReadKey();
                if (key.KeyChar == 57) app = false;
            }
            scheduler.Dispose();
            storage.Dispose();
            Console.WriteLine("App terminated!");
            Console.WriteLine("Press 9 to start the app once again!");
            bool appcon = true;
            while (appcon)
            {
                var key = Console.ReadKey();
                if (key.KeyChar == 57) app = false;
            }

            Continue();
        }

        private static void Continue()
        {
            var storage = new SimpleFileStorageEngine("./Hello.txt",false);
            var engine = ExecutionEngineFactory.Continue(storage);

            while(true)
            {
                Console.WriteLine("Press 9 to stop app");
                bool app = true;
                while (app)
                {
                    var key = Console.ReadKey();
                    if (key.KeyChar == 57) app = false;
                }
                engine.Dispose();
                Console.WriteLine("App terminated!");
                Console.WriteLine("Press 9 to start the app once again!");
                bool appcon = true;

                while (appcon)
                {
                    var key = Console.ReadKey();
                    if (key.KeyChar == 57) app = false;
                }
                engine = ExecutionEngineFactory.Continue(storage);
            }
        }
    }
}