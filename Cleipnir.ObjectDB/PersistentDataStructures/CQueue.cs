using System;
using System.Collections;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class CQueue<T> : IPersistable, IEnumerable<T>
    {
        private readonly LinkedList<T> _queue;

        private readonly List<T> _addedValues = new List<T>();
        private int _headId;
        private int _tailId;
        private int _removed;

        public CQueue() => _queue = new LinkedList<T>();

        private CQueue(LinkedList<T> queue, int headId, int tailId)
        {
            _queue = queue;
            _headId = headId;
            _tailId = tailId;
        }

        public int Count => _queue.Count;

        public void Enqueue(T value)
        {
            _queue.AddLast(value);
            _addedValues.Add(value);
        }

        public T Dequeue()
        {
            if (_queue.Count == 0) throw new InvalidOperationException("Queue is empty");

            var dequeued = _queue.First.Value;
            _queue.RemoveFirst();
            _removed++;
            _headId++;

            return dequeued;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_removed == 0 && _addedValues.Count == 0) return;

            foreach (var addedValue in _addedValues)
                sd.Set(_tailId++ + "_Value", addedValue);

            _addedValues.Clear();

            for (var i = 1; i <= _removed; i++)
                sd.Set((_headId - i) + "_Value", null);

            _removed = 0;

            sd.Set(nameof(_headId), _headId);
            sd.Set(nameof(_tailId), _tailId);
        }

        private static CQueue<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            if (!sd.ContainsKey(nameof(_headId))) return new CQueue<T>();

            var headId = (int) sd[nameof(_headId)];
            var tailId = (int) sd[nameof(_tailId)];

            if (headId == tailId) return new CQueue<T>();
            
            var inner = new LinkedList<T>();

            for (var curr = headId; curr < tailId; curr++)
            {
                var value = (T) sd[curr + "_Value"];
                inner.AddLast(value);
            }

            return new CQueue<T>(inner, headId, tailId);
        }

        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
