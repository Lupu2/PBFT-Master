using System.Collections.Generic;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.SimulateSendReceive
{
    public class Work : IPersistable
    {
        public int duration{get; set;}

        public int earning{get; set;}
        public string name{get; set;}
        public Worker assignedworker{get; set;}
        public bool finished{get; set;}
        public Work(int dura, string desc, int money)
        {
            duration = dura;
            name = desc;
            earning = money;
        }
        
        public Work(int dura, string desc, int money, Worker assworker, bool done)
        {
            duration = dura;
            name = desc;
            earning = money;
            assignedworker = assworker;
            finished = done;
        }

        public async CTask PerformWork(Worker assworker)
        {
            //assignedworker = assworker;
            await Sleep.Until(duration);
            finished = true;
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(duration), duration);
            stateToSerialize.Set(nameof(name), name);
            stateToSerialize.Set(nameof(earning), earning);
            stateToSerialize.Set(nameof(assignedworker), assignedworker);
            stateToSerialize.Set(nameof(finished), finished);
        }

        private static Work Deserialize(IReadOnlyDictionary<string, object> sd) 
            => new Work((int) sd[nameof(duration)], (string) sd[nameof(name)], (int) sd[nameof(earning)], (Worker) sd[nameof(assignedworker)], (bool) sd[nameof(finished)]);

        public override string ToString() => $"Job: {name}, Worker: {assignedworker.name}, TimeDuration: {duration}, Earning: {earning}, Finished: {finished}";
    }
}