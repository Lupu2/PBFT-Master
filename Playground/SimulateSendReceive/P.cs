using System;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground.SimulateSendReceive
{
    public static class P
    {
        public static void Do() => StartNew();

        public static void StartNew()
        {
            var storage = new SimpleFileStorageEngine("./Simulate.txt",true);
            var scheduler = ExecutionEngineFactory.StartNew(storage);
            var tm = new TaskMaster();
            scheduler.Schedule(() => {
               Task.Run(() => Console.WriteLine("Test"));
               var workercomm = new Source<Work>(); 
               var mastercomm = new Source<Work>();
               tm.Contactmedium = mastercomm;
               var worker = new Worker{ Contactmedium = workercomm, name = "George"};
               var worker2 = new Worker{ Contactmedium = workercomm, name = "Jeremy"};
               var wa = new WorkAssigner{ MasterChannel = mastercomm, 
                                          WorkerChannel = workercomm, 
                                          WorkerList = new CList<Worker>(){worker,worker2}};
               _ = worker.Start();
               _ = worker2.Start();
               _ = wa.Start();
               _ = tm.Start();
              
            });

            Console.WriteLine("Press 9 to stop app");
            bool app = true;
            while (app)
            {
                var key = Console.ReadKey();
                if (key.KeyChar == 57) app = false;
            }
            //tm.DisplayWorkList();
            scheduler.Dispose();
            storage.Dispose();
            Console.WriteLine("App terminated!");
            Continue();
        }

        public static void Continue()
        {
            Console.WriteLine("Press enter to continue app");
            Console.ReadLine();

            var storage = new SimpleFileStorageEngine("./Simulate.txt",false);
            
            while(true)
            {
                var scheduler = ExecutionEngineFactory.Continue(storage);
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
                Console.WriteLine("Press enter to continue app");
                Console.ReadLine();
            }
        }
    }
}