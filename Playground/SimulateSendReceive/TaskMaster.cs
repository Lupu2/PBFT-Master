using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
    public class TaskMaster : IPersistable
    {

        public CList<Work> FinishedTaskList = new CList<Work>();
        
        //public CList<Worker> WorkerList = new CList<Worker>();
        public Source<Work> Contactmedium;

        public Source<Work> Readermedium;
        public Random rng = new Random();

        public async CTask Start()
        {   await Sleep.Until(2000);
            Readermedium = new Source<Work>();
            _ = CreateWork();
            while(true)
            {
                Work work = await Readermedium.Next();
                Contactmedium.Emit(work);
                Work finwork = await Contactmedium.Where(m => m.finished).Next();
                FinishedTaskList.Add(finwork);
                DisplayWorkList();    
            }
        }

        public async CTask CreateWork()
        {
            while(true)
            {
                Console.WriteLine("Create a task:");
                string name = Console.ReadLine();
                Console.WriteLine("Name: " +name);
                int dura = rng.Next(500,5000);
                int earning = rng.Next(100,1000);
                Work work = new Work(dura,name,earning);
                await Sleep.Until(2000); //Time it takes to assign the job
                Readermedium.Emit(work);
            }
        }

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(FinishedTaskList), FinishedTaskList);
            //stateToSerialize.Set(nameof(WorkerList),WorkerList);
            stateToSerialize.Set(nameof(Contactmedium), Contactmedium);
            stateToSerialize.Set(nameof(Readermedium), Readermedium);
        }

        private static TaskMaster Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new TaskMaster()
            {
                FinishedTaskList = sd.Get<CList<Work>>(nameof(FinishedTaskList)),
                Contactmedium = sd.Get<Source<Work>>(nameof(Contactmedium)),
                Readermedium = sd.Get<Source<Work>>(nameof(Readermedium)),
                rng = new Random() //doesn't need to be saved, but unsure if it will be assigned
            };
        }

        public void DisplayWorkList()
        { 
            foreach (var finwork in FinishedTaskList)
            {
                Console.WriteLine(finwork);
            }
        }
    }
}