using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class ImmutableCList<T> : IPersistable, IEnumerable<T>
    {
        private readonly T[] _inner;
        private bool _serialized;

        public ImmutableCList(T[] inner) => _inner = inner;

        public T this[int index] => _inner[index];
        public int Count => _inner.Length;

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; _serialized = true;

            sd.Set("Length", _inner.Length);
            for (var i = 0; i < _inner.Length; i++)
                sd.Set(i.ToString(), _inner[i]);
        }

        private static ImmutableCList<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var length = sd.Get<int>("Length");
            var inner = new T[length];

            for (var i = 0; i < length; i++)
                inner[i] = sd.Get<T>(i.ToString());

            return new ImmutableCList<T>(inner) {_serialized = true};
        }

        public IEnumerator<T> GetEnumerator() => _inner.Select(_ => _).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}