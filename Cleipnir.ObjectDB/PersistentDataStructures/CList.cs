using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class CList<T> : IPersistable, IEnumerable<T>
    {
        private readonly List<Node> _inner;

        private readonly List<Node> _removedNodes = new List<Node>();
        private readonly List<Node> _changedNodes = new List<Node>();
        private long _maxId;

        public CList() => _inner = new List<Node>();

        private CList(List<Node> inner, long maxId)
        {
            _inner = inner;
            _maxId = maxId;
        }

        public T this[int index]
        {
            get => _inner[index].Value;
            set => Set(index, value);
        }

        public int Count => _inner.Count;

        public void Add(T elem)
        {
            var node = new Node(_maxId++) {Value = elem};
            _inner.Add(node);
            _changedNodes.Add(node);

            if (_inner.Count > 1)
                _inner[^2].Next = node;
        }

        public void Set(int index, T value)
        {
            _inner[index].Value = value;
            _changedNodes.Add(_inner[index]);
        }

        public void Remove(int index)
        {
            if (_inner.Count <= index || index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            var nodeToRemove = _inner[index];
            MakePrevNodeJumpOverAtIndex(index);
            if (index != 0)
                _changedNodes.Add(_inner[index-1]);
            
            _inner.RemoveAt(index);

            _removedNodes.Add(nodeToRemove);
        }

        private void MakePrevNodeJumpOverAtIndex(int index)
        {
            if (index == 0) return;

            var prev = _inner[index - 1];

            var next = index + 1 >= _inner.Count ? default : _inner[index + 1];

            prev.Next = next;
        }

        private class Node
        {
            public Node(long id) => Id = id;

            public T Value { get; set; }
            public long Id { get; }

            public Node Next { get; set; }

            public void Serialize(StateMap sd)
            {
                sd.Set(Id + "_Value", Value);
                sd.Set(Id + "_NextId", Next?.Id);
            }

            public void Delete(StateMap sd)
            {
                sd.Set(Id + "_Value", null);
                sd.Set(Id + "_NextId", null);
            }

            public static Tuple<Node, long?> Deserializer(long id, IReadOnlyDictionary<string, object> sd)
            {
                var value = (T) sd[id + "_Value"];
                var nextId = (long?) sd[id + "_NextId"];

                return Tuple.Create(new Node(id) { Value = value }, nextId);
            }
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_changedNodes.Count == 0 && _removedNodes.Count == 0) return;
            
            sd.Set("RootId", _inner.Count == 0 ? null : (object)_inner[0].Id);
            sd.Set("MaxId", _maxId);

            foreach (var changedNode in _changedNodes)
                changedNode.Serialize(sd);

            foreach (var removedNode in _removedNodes)
                removedNode.Delete(sd);

            _changedNodes.Clear();
            _removedNodes.Clear();
        }

        private static CList<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            if (!sd.ContainsKey("RootId") || sd["RootId"] == null) return new CList<T>();

            var maxId = (long) sd["MaxId"];
            var nextId = (long?) sd["RootId"];
            var inner = new List<Node>();

            while (nextId != null)
            {
                var (node, id) = Node.Deserializer(nextId.Value, sd);
                nextId = id;
                inner.Add(node);
            }

            return new CList<T>(inner, maxId);
        }

        public IEnumerator<T> GetEnumerator() => _inner.Select(n => n.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
