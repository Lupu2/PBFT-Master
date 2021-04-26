using System;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground.PersonExample
{
    internal static class P
    {
        public static void Do()
        {
            var storageEngine = new SimpleFileStorageEngine("./persons.jsons", true);
            var os = ObjectStore.New(storageEngine);
            var sib1 = new Person()
            {
                Name = "Test"
            };
            var sib2 = new Person()
            {
                Name = "Test2"
            };
            var parent = new Person()
            {
                Name = "Ole"
            };
            
            var child = new Person()
            {
                Name = "Peter",
                Parent = parent
            };
           
            CList<Person> siblings = new CList<Person>();
            siblings.Add(sib1);
            siblings.Add(sib2);
            parent.Siblings = siblings;
            Console.WriteLine("Attaching");
            os.Attach(parent);
            os.Attach(child);
            Console.WriteLine("Persisting");
            os.Persist();
            os = ObjectStore.Load(storageEngine);
            Console.WriteLine("Finished loading");
            var p2 = os.ResolveAll<Person>();
            Console.WriteLine("resolving");
            foreach (var p in p2)
            {
                Console.WriteLine(p + "\n");
            }
            
        }
    }
}