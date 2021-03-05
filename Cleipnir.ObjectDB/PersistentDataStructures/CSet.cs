using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class CSet<T> : IPersistable, IEnumerable<T>
    {
        private readonly ISet<Node> _changedNodes = new HashSet<Node>();
        private readonly ISet<Node> _removedNodes = new HashSet<Node>();

        private readonly Dictionary<T, Node> _nodes;
        private Node Root { get; set; }

        private int _maxId = -1;

        public CSet() => _nodes = new Dictionary<T, Node>();

        private CSet(Dictionary<T, Node> nodes, Node root, int maxId)
        {
            _nodes = nodes;
            _maxId = maxId;
            Root = root;
        }

        public int Count => _nodes.Count;

        public bool Add(T toAdd)
        {
            if (_nodes.ContainsKey(toAdd)) return false;

            var id = ++_maxId;
            var node = new Node(id, toAdd);
            
            if (Count == 0)
                Root = node;
            else
            {
                var next = Root;
                Root = node;
                next.Prev = node;
                node.Next = next;
            }

            _nodes[toAdd] = node;

            _changedNodes.Add(node);
            return true;
        }

        public void Remove(T toRemove)
        {
            if (!_nodes.ContainsKey(toRemove)) return;

            var nodeToRemove = _nodes[toRemove];
            _nodes.Remove(toRemove);
            _removedNodes.Add(nodeToRemove);

            var prev = nodeToRemove.Prev;
            var next = nodeToRemove.Next;
            if (prev != null)
            {
                prev.Next = next;
                _changedNodes.Add(prev);
            }
                
            if (next != null)
                next.Prev = prev;

            if (Root.Value.Equals(toRemove))
                Root = Root.Next;
        }

        public bool Contains(T t) => _nodes.ContainsKey(t);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_changedNodes.Empty() && _removedNodes.Empty()) return;

            sd.Set(nameof(Root), Root?.Id);

            foreach (var changedNode in _changedNodes)
                changedNode.Serialize(sd);

            foreach (var removedNode in _removedNodes)
                removedNode.Remove(sd);

            _removedNodes.Clear();
            _changedNodes.Clear();
        }

        internal static CSet<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            if (!sd.ContainsKey(nameof(Root)) || sd[nameof(Root)] == null)
                return new CSet<T>();

            var nodes = new Dictionary<T, Node>();

            var rootId = (int) sd[nameof(Root)];

            var (root, rootNextId) = Node.Deserialize(rootId, sd, null);
            nodes[root.Value] = root;

            var prev = root;
            var currId = rootNextId;

            while (currId != null)
            {
                var (curr, nextId) = Node.Deserialize(currId.Value, sd, prev);
                nodes[curr.Value] = curr;

                prev = curr;
                currId = nextId;
            }

            return new CSet<T>(nodes, root, rootId);
        }

        private class Node 
        {
            public Node(int id, T value)
            {
                Id = id;
                Value = value;
            }

            public Node Next { get; set; }
            public Node Prev { get; set; }
            public T Value { get; }
            public int Id { get;  }

            public void Serialize(StateMap sd)
            {
                sd.Set("Value_" + Id, Value);
                sd.Set("Next_" + Id, Next?.Id);
            }

            public void Remove(StateMap sd)
            {
                sd.Remove($"Value_{Id}");
                sd.Remove($"Next_{Id}");
            }

            public static Tuple<Node, int?> Deserialize(int id, IReadOnlyDictionary<string, object> sd, Node prev)
            {
                var value = (T) sd["Value_" + id];
                var next = (int?) sd["Next_" + id];
                var node = new Node(id, value) {Prev = prev};
                if (prev != null)
                    prev.Next = node;

                return Tuple.Create(node, next);
            }
        }

        public IEnumerator<T> GetEnumerator() => _nodes.Values.Select(n => n.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
