using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.PersonExample
{
    internal class Person : IPersistable
    {
        public string Name { get; set; }
        public Person Parent { get; set; }
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd[nameof(Name)] = Name;
            sd[nameof(Parent)] = Parent;
        }

        private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var name = sd.Get<string>(nameof(Name));
            var parent = sd.Get<Person>(nameof(Parent));
            return new Person
            {
                Name = name,
                Parent = parent
            };
        }
    }
}