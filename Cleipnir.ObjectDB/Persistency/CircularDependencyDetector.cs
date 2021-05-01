using System.Collections.Immutable;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.Persistency
{
    internal class CircularDependencyDetector
    {
        public ImmutableList<object> Check(ISerializer root, StateMaps stateMaps) 
            => Check(root, ImmutableList<ISerializer>.Empty, stateMaps)
                .Select(s => s.Instance)
                .ToImmutableList();

        private ImmutableList<ISerializer> Check(ISerializer curr, ImmutableList<ISerializer> visited, StateMaps stateMaps)
        {
            if (visited.Contains(curr))
                return visited.Append(curr).Aggregate(
                    ImmutableList<ISerializer>.Empty,
                    (l, s) => l.Any() || s == curr ? l.Add(s) : l
                );

            visited = visited.Add(curr);
            
            foreach (var serializer in stateMaps.Get(curr.Id).GetReferencedSerializers())
            {
                var circularChain = Check(serializer, visited, stateMaps);
                if (circularChain.Count > 0)
                    return circularChain;
            }

            return ImmutableList<ISerializer>.Empty;
        }
    }
}