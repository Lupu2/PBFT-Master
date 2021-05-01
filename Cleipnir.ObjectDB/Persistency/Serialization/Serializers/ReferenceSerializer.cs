using System.Collections.Generic;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class ReferenceSerializer : ISerializer
    {
        public long Id { get; }
        public object Instance => Reference;
        internal Reference Reference { get; }

        public ReferenceSerializer(long id, Reference reference)
        {
            Id = id;
            Reference = reference;
        }

        public void Serialize(StateMap sd, SerializationHelper helper) => Reference.Serialize(sd, helper);

        private static ReferenceSerializer Deserialize(long id, IReadOnlyDictionary<string, object> sd)
            => new ReferenceSerializer(id, Reference.Deserialize(sd));
    }
}
