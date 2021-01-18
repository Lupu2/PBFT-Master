using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Cleipnir.GroupNetworkCommunication
{
    public class MessageQueue : IPersistable
    {
        private CDictionary<int, ImmutableByteArray> Queue { get; init; } = new();
        private int _atIndex;
        private bool _isSerialized;

        public void Add(ImmutableByteArray bytes, CTask removeWhen)
        {
            var key = _atIndex++;
            var remover = new Remover(key, Queue);
            removeWhen.Awaitable.GetAwaiter().OnCompleted(remover.Remove);
            Queue[key] = bytes;
        }

        public IEnumerable<ImmutableByteArray> GetAll() => Queue
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value);
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_isSerialized) return; _isSerialized = true;

            sd.Set(nameof(Queue), Queue);
        }
        
        private static MessageQueue Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var queue = sd.Get<CDictionary<int, ImmutableByteArray>>(nameof(Queue));
            if (queue.Count == 0) return new MessageQueue();

            var atIndex = queue.Keys.Max() + 1;
            return new() { Queue = queue, _atIndex = atIndex, _isSerialized = true };
        }

        private class Remover : IPersistable
        {
            public Remover(int index, CDictionary<int, ImmutableByteArray> queue)
            {
                Index = index;
                Queue = queue;
            }

            public int Index { get; }
            public CDictionary<int, ImmutableByteArray> Queue { get; }
            private bool _isSerialized = false;

            public void Remove() => Queue.Remove(Index);

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_isSerialized) return; _isSerialized = true;
                
                sd.Set(nameof(Index), Index);
                sd.Set(nameof(Queue), Queue);
            }

            private static Remover Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new Remover(
                    sd.Get<int>(nameof(Index)),
                    sd.Get<CDictionary<int, ImmutableByteArray>>(nameof(Queue))
                );
            }
        }
    }
    
}