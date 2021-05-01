using System.Collections;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class CAppendOnlyList<T> : IPersistable, IEnumerable<T>
    {
        private readonly List<T> _inner;

        private readonly List<T> _addeds = new List<T>();

        public CAppendOnlyList() => _inner = new List<T>();
        public CAppendOnlyList(int initialSize) => _inner = new List<T>(initialSize);

        private CAppendOnlyList(List<T> inner) => _inner = inner;

        public int Count => _inner.Count;

        public void Add(T toAdd)
        {
            _addeds.Add(toAdd);
            _inner.Add(toAdd);
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_addeds.Count == 0) return;

            var addedCount = _addeds.Count;
            var totalCount = _inner.Count;
            var offset = totalCount - addedCount;

            for (var i = 0; i < addedCount; i++)
                sd.Set((i + offset).ToString(), _addeds[i]);

            _addeds.Clear();
        }

        private static CAppendOnlyList<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var i = 0;
            var inner = new List<T>();
            while (sd.ContainsKey(i.ToString()))
            {
                inner.Add(sd.Get<T>(i.ToString()));
                i++;
            }

            return new CAppendOnlyList<T>(inner);
        }

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
