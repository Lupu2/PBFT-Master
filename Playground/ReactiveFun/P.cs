using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;

namespace Playground.ReactiveFun
{
    public static class P
    {
        public static void Do()
        {
            var storageEngine = new InMemoryStorageEngine();
            //var os = ObjectStore.New(storageEngine);
            var executionEngine = ExecutionEngineFactory.StartNew(storageEngine);

            executionEngine.Schedule(() =>
            {
                var source = new Source<object>();
                Roots.Entangle(source);
                var proposer = new PaxosProposer(source);
                _ = proposer.StartRound("hello", 0);
            });

            
            executionEngine.Schedule(() =>
            {
                var source = Roots.Resolve<Source<object>>();
                source.Emit(new Promise() {Ballot = 0});
                source.Emit(new Promise() {Ballot = 0});
                source.Emit(new Promise() {Ballot = 0});
            });
            
            Thread.Sleep(1000);
            
            executionEngine.Schedule(() =>
            {
                var source = Roots.Resolve<Source<object>>();
                source.Emit(new Accepted() {});
                source.Emit(new Accepted() {});
                source.Emit(new Accepted() {});
            });
            
            Thread.Sleep(100_000);
            Console.WriteLine();
            Console.WriteLine();
        }

        public class PaxosProposer : IPersistable
        {
            private Stream<object> Messages { get; }
            public CList<object> Broadcast { get; } = new CList<object>();

            private const int Quorum = 3;

            public PaxosProposer(Stream<object> messages) => Messages = messages;

            public async CTask DecideProposal(string proposal)
            {
                throw new NotImplementedException();
            }

            public async CTask StartRound(string proposal, int ballot)
            {
                CAppendOnlyList<Promise> promList = new CAppendOnlyList<Promise>();
                Broadcast.Add(new Prepare {Ballot = ballot});
                var promises = await Messages //IENUMERABLE of Promises 
                    //.OfType<Promise>()
                    .Where(m => m is Promise)
                    .Scan(promList, (l, p) => { l.Add((Promise) p); return l; })
                    .Where(l => l.Count == Quorum)
                    .Next();
                Console.WriteLine(promList.Count);
                var toPropose = promises
                    .Append(new Promise {PromisedBallot = -1, PromisedValue = proposal})
                    .Where(p => p.PromisedBallot != null)
                    .OrderByDescending(p => p.PromisedBallot)
                    .First()
                    .PromisedValue;

                Broadcast.Add(new Accept() {Ballot = ballot, Value = toPropose});
                CAppendOnlyList<Accepted> accedList = new CAppendOnlyList<Accepted>();
                Console.WriteLine(accedList.Count);
                
                await Messages
                    //.OfType<Accepted>()
                    .Where(m => m is Accepted)
                    .Scan(accedList, (l, a) => { l.Add((Accepted) a); return l; })
                    .Where(l => l.Count == Quorum)
                    .Next();
                Console.WriteLine(accedList.Count);
                Console.WriteLine("COMPLETED!!!");
            }

            public void Serialize(StateMap sd, SerializationHelper helper) { }

            private static PaxosProposer Deserialize(IReadOnlyDictionary<string, object> sd)
                => throw new NotImplementedException();
        }

        public class Prepare : IPropertyPersistable
        {
            public int Ballot { get; set; }
        }

        public class Promise: IPropertyPersistable
        {
            public int Ballot { get; set; }
            public int? PromisedBallot { get; set; }
            public string PromisedValue { get; set; }
        }

        public class Accept : IPropertyPersistable
        {
            public int Ballot { get; set; }
            public string Value { get; set; }
        }
        public class Accepted : IPropertyPersistable 
        {}
    }
}
