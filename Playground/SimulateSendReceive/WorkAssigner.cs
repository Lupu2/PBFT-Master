
using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;

namespace Playground.SimulateSendReceive
{
    public class WorkAssigner : IPersistable
    {
        public Source<Work> MasterChannel;
        public Source<Work> WorkerChannel;

        public CList<Worker> WorkerList;

        public CDictionary<string, bool> WorkerStatus = new CDictionary<string, bool>();

        public async CTask Start()
        {
            InitializeWorkerStatus();
            _ = DeliverReport();
            while (true)
            {
                var newwork = await MasterChannel.Where(m => !m.finished).Next();
                Console.WriteLine("Got task");
                var worker = AssignWork();
                newwork.assignedworker = worker;
                Console.WriteLine($"Assined worker: {newwork.assignedworker.name}");
                WorkerChannel.Emit(newwork);
                Console.WriteLine("emitted work to worker");
                //DeliverReport...
                /*var finwork = await WorkerChannel.Next();
                Console.WriteLine("got finished work");
                WorkerStatus[finwork.assignedworker.name] = false;
                await Sleep.Until(1000);
                MasterChannel.Emit(finwork);*/
            }   
        }
        public Worker AssignWork()
        {   
            Start:
            Worker givenworker = new Worker{};
            foreach(var worker in WorkerList)
            {
                //Console.WriteLine("Hello puppy");
                if (!WorkerStatus[worker.name]) 
                {
                    WorkerStatus[worker.name] = true;
                    givenworker = worker;
                    break;
                }
            }
            //Console.WriteLine(givenworker);
            if (givenworker.name.Equals("")) goto Start;
            return givenworker;
        }

        public async CTask DeliverReport()
        {
            while (true)
            {   
                Console.WriteLine("Listening for jobs");
                var finwork = await WorkerChannel.Where(m => m.finished).Next();
                WorkerStatus[finwork.assignedworker.name] = false;
                await Sleep.Until(1000);
                MasterChannel.Emit(finwork);
            }
        }

        private void InitializeWorkerStatus() 
        {
            foreach (var Worker in WorkerList) WorkerStatus[Worker.name] = false;
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(MasterChannel), MasterChannel);
            stateToSerialize.Set(nameof(WorkerChannel), WorkerChannel);
            stateToSerialize.Set(nameof(WorkerList), WorkerList);
            stateToSerialize.Set(nameof(WorkerStatus), WorkerStatus);
        }

        private static WorkAssigner Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new WorkAssigner{
                MasterChannel = sd.Get<Source<Work>>(nameof(MasterChannel)),
                WorkerChannel = sd.Get<Source<Work>>(nameof(WorkerChannel)),
                WorkerList = sd.Get<CList<Worker>>(nameof(WorkerList)),
                WorkerStatus = sd.Get<CDictionary<string,bool>>(nameof(WorkerStatus))
            };
        }
    }
}