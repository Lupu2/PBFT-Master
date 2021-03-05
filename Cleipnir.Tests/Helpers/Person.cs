using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Tests.Helpers
{
    public class Person : IPersistable
    {
        public string Name { get; set; }
        public Person Other { get; set; }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(Name), Name);
            sd.Set(nameof(Other), Other);
        }

        private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
            => new Person() { Name = sd.Get<string>(nameof(Name)), Other = sd.Get<Person>(nameof(Other)) };
    }
}
