using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Helpers.DataStructures;

namespace Cleipnir.StorageEngine
{
    public class GarbageCollector 
    {
        private readonly DictionaryWithDefault<long, Node> _nodes;

        private bool _color;

        public GarbageCollector()
        {
            _nodes = new DictionaryWithDefault<long, Node>(id => new Node(id) {Color = _color});
        }

        public IEnumerable<long> Collect(IEnumerable<StorageEntry> storageEntries, IEnumerable<ObjectIdAndKey> removedEntries)
        {
            UpdateGraphFromEntries(storageEntries);
            RemoveRemovedEntries(removedEntries);

            return FindGarbageCollectables();
        }

        private void UpdateGraphFromEntries(IEnumerable<StorageEntry> entries)
        {
            var allObjectIds = entries
                .Select(e => e.ObjectId)
                .Distinct();

            foreach (var objectId in allObjectIds)
                _nodes.AddIfNotExists(objectId);

            var frameworkReferenceIds = entries
                .Where(e => e.Value != null)
                .Where(e => e.Value.ToString().Contains("ReferenceSerializer"))
                .Select(e => long.Parse(e.Key))
                .ToHashSet();

            var frameworkReferenceFromAndTos = entries
                .Where(e => e.Key == "Id" && e.Value != null && frameworkReferenceIds.Contains(e.ObjectId))
                .Select(e => new { e.ObjectId, ReferenceTo = (long)e.Value });

            foreach (var frameworkRef in frameworkReferenceFromAndTos)
                _nodes[frameworkRef.ObjectId].References["FrameworkRef"] = _nodes[frameworkRef.ReferenceTo];

            foreach (var commonReference in entries)
            {
                if (commonReference.Reference.HasValue)
                    _nodes[commonReference.ObjectId].References[commonReference.Key] = _nodes[commonReference.Reference.Value];
                else
                    _nodes[commonReference.ObjectId].References.Remove(commonReference.Key);
            }
        }

        private void RemoveRemovedEntries(IEnumerable<ObjectIdAndKey> removedEntries)
        {
            foreach (var (objectId, key) in removedEntries)
                _nodes[objectId].References.Remove(key);
        }

        private List<long> FindGarbageCollectables()
        {
            var visited = new HashSet<long>();
            _color = !_color;

            var toVisit = new Queue<Node>();
            toVisit.Enqueue(_nodes[0]);

            while (toVisit.Count > 0)
            {
                var node = toVisit.Dequeue();
                if (visited.Contains(node.Id)) continue;
                visited.Add(node.Id);

                node.Color = _color;
                foreach (var neighbor in node.References.Values)
                    toVisit.Enqueue(neighbor);
            }

            var garbageCollectables = _nodes.Values.Where(n => n.Color != _color).Select(n => n.Id).ToList();

            foreach (var gc in garbageCollectables)
                _nodes.Remove(gc);

            return garbageCollectables;
        }

        private class Node
        {
            public Node(long id) => Id = id;

            public long Id { get; }
            public bool Color { get; set; }
            public Dictionary<string, Node> References { get; set; } = new Dictionary<string, Node>();

            public override string ToString() => $"{Id} -> {string.Join($" | {Id} -> ", References)}";
        }
    }
}
