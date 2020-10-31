using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class CDictionary<TKey, TValue> : IPersistable, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, Node> _inner;

        private readonly List<Node> _changedNodes = new List<Node>();
        private readonly List<Node> _deletedNodes = new List<Node>();

        private long _maxId;
        private Node Tail { get; set; }

        public CDictionary() => _inner = new Dictionary<TKey, Node>();

        private CDictionary(Dictionary<TKey, Node> inner, long maxId, Node tail)
        {
            _inner = inner;
            _maxId = maxId;
            Tail = tail;
        }

        public TValue this[TKey key]
        {
            get => _inner[key].Value;
            set => Set(key, value);
        }

        public int Count => _inner.Count;
        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public IEnumerable<TValue> Values => _inner.Values.Select(n => n.Value);

        public void Set(TKey key, TValue value)
        {
            if (_inner.ContainsKey(key))
            {
                var node = _inner[key];
                node.Value = value;
                _changedNodes.Add(node);
            }
            else
            {
                var node = new Node(_maxId++, key) {Value = value};
                if (Tail != null)
                    Tail.Next = node;

                node.Prev = Tail;
                Tail = node;

                _inner[key] = node;
                _changedNodes.Add(node);
            }
        }

        public void Remove(TKey key)
        {
            if (!_inner.ContainsKey(key)) return;

            var node = _inner[key];

            if (node.Prev != null)
                node.Prev.Next = node.Next;

            if (node.Next != null)
                node.Next.Prev = node.Prev;
            else
                Tail = node.Prev;

            _inner.Remove(key);
            _deletedNodes.Add(node);

            if (node.Next != null)
                _changedNodes.Add(node.Next);
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_changedNodes.Count == 0 && _deletedNodes.Count == 0)
                return;

            sd.Set("Tail_Id", Tail?.Id);
            sd.Set(nameof(_maxId), _maxId);

            foreach (var changedNode in _changedNodes)
                changedNode.Serialize(sd);

            foreach (var deletedNode in _deletedNodes)
                deletedNode.Remove(sd);

            _changedNodes.Clear();
            _deletedNodes.Clear();
        }

        private static CDictionary<TKey, TValue> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
                if (!sd.ContainsKey("Tail_Id")) return new CDictionary<TKey, TValue>();

                var tailId = (long?) sd["Tail_Id"];
                if (tailId == null) return new CDictionary<TKey, TValue>();

                var maxId = (long) sd[nameof(_maxId)];

                var inner = new Dictionary<TKey, Node>();

                var (tail, currId) = Node.Deserialize(tailId.Value, sd);
                inner[tail.Key] = tail;

                var next = tail;
                while (currId.HasValue)
                {
                    var tuple = Node.Deserialize(currId.Value, sd);
                    var node = tuple.Item1;
                    inner[node.Key] = node;
                    node.Next = next;
                    next.Prev = node;

                    next = node;
                    currId = tuple.Item2;
                }

                return new CDictionary<TKey, TValue>(inner, maxId, tail);
        }

        private class Node
        {
            public Node(long id, TKey key)
            {
                Id = id;
                Key = key;
            }

            public long Id { get; }
            public TKey Key { get; }
            public TValue Value { get; set; }
            public Node Next { get; set; }
            public Node Prev { get; set; }

            public void Serialize(StateMap sd)
            {
                sd.Set(Id + "_" + nameof(Key), Key);
                sd.Set(Id + "_" + nameof(Value), Value);
                sd.Set(Id + "_" + nameof(Prev), Prev?.Id);
            }

            public static Tuple<Node, long?> Deserialize(long id, IReadOnlyDictionary<string, object> sd)
            {
                var key = (TKey) sd[id + "_" + nameof(Key)];
                var value = (TValue) sd[id + "_" + nameof(Value)];
                var prev = (long?) sd[id + "_" + nameof(Prev)];
                return Tuple.Create(new Node(id, key) {Value = value}, prev);
            }

            public void Remove(StateMap sd)
            {
                sd.Set(Id + "_" + nameof(Key), null);
                sd.Set(Id + "_" + nameof(Value), null);
                sd.Set(Id + "_" + nameof(Prev), null);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _inner.Select(kv => new KeyValuePair<TKey, TValue>(kv.Key, kv.Value.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
