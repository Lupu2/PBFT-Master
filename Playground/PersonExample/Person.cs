using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace Playground.PersonExample
{
    internal class Person : IPersistable
    {
        public string Name { get; set; }
        public Person Parent { get; set; }
        public CList<Person> Siblings { get; set; }
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd[nameof(Name)] = Name;
            sd[nameof(Parent)] = Parent;
            sd[nameof(Siblings)] = Siblings;
        }

        private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var name = sd.Get<string>(nameof(Name));
            var parent = sd.Get<Person>(nameof(Parent));
            var children = sd.Get<CList<Person>>(nameof(Siblings));
            return new Person
            {
                Name = name,
                Parent = parent,
                Siblings = children
            };
        }

        public override string ToString()
        {
            string person =  $"Name: {Name}, ";
            if (Parent != null) person += $"Parent: {Parent.Name}, ";
            person += "Children: \n";
            if (Siblings != null)
            {
                foreach (var c in Siblings)
                    person += $"Name: {c.Name}\n";
            }
            return person;
        }
    }
}