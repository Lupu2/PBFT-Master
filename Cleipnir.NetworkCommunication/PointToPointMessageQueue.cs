using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace Cleipnir.NetworkCommunication
{
    internal class PointToPointMessageQueue : IPersistable
    {
        private readonly CDictionary<int, ImmutableByteArray> _inner;
        private bool _serialized;

        public PointToPointMessageQueue(CDictionary<int, ImmutableByteArray> inner) => _inner = inner;

        public ImmutableByteArray this[int index]
        {
            get => _inner[index];
            set => _inner[index] = value;
        }

        public void Remove(int index) => _inner.Remove(index);
        public bool ContainsKey(int index) => _inner.ContainsKey(index);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; _serialized = true;

            sd.Set(nameof(_inner), _inner);
        }

        private static PointToPointMessageQueue Deserialize(IReadOnlyDictionary<string, object> sd) 
            => new PointToPointMessageQueue(sd.Get<CDictionary<int, ImmutableByteArray>>(nameof(_inner))) {_serialized = true};
    }
}
