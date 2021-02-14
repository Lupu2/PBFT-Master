using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var storageEngine = new SimpleFileStorageEngine(@"./action.txt", true);
            var os = ObjectStore.New(storageEngine);

            var person = new Person() {Name = "Peter", Awaitable = new CAwaitable()};
            //Action<object> greet = person.Greet;

            //var casted = (Action<string>) greet;
            //casted("OK");

            os.Attach(person);

            _ = person.DoStuff();
            
            os.Persist();

            //greet("Hello");
            
            os = ObjectStore.Load(storageEngine);
            //greet = os.Resolve<Action<string>>();

            //greet("Hello again");

            person = os.Resolve<Person>();
            person.Awaitable.SignalCompletion();
            
            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }

    public class Person : IPersistable
    {
        public string Name { get; set; }

        public CAwaitable Awaitable { get; set; }
        public void Greet(object greeting)
        {
            Console.WriteLine($"{Name} says {greeting}");
        }

        public async CTask DoStuff()
        {
            await Awaitable;
            Console.WriteLine("OK AFTER");
        }
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd[nameof(Name)] = Name;
            sd[nameof(Awaitable)] = Awaitable;
        }

        private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var name = sd.Get<string>(nameof(Name));
            var awaitable = sd.Get<CAwaitable>(nameof(Awaitable));
            return new Person() { Name = name, Awaitable = awaitable };   
        }
    }
}
