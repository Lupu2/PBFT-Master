using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace Cleipnir.NetworkCommunication
{
    internal class OutgoingMessageQueue : IPersistable
    {
        private CDictionary<int, ImmutableByteArray> _unackedQueue;
        private int _preSyncHead;
        internal int Head { get; private set; }
        internal int Tail { get; private set; }

        public OutgoingMessageQueue()
        {
            _unackedQueue = new();
            Tail = 0;
            Head = 0;
            _preSyncHead = 0;
        }

        public int Enqueue(ImmutableByteArray item) //todo return index to allow for cancellation
        {
            _unackedQueue[_preSyncHead] = item;
            _preSyncHead++;

            return _preSyncHead - 1;
        }

        public void AckUntil(int until)
        {
            for (;Tail <= until; Tail++)
                _unackedQueue.Remove(Tail);
        }

        public IEnumerable<Tuple<int, ImmutableByteArray>> GetSyncedUnackedMessages()
        {
            for (var i = Tail; i < Head; i++)
                yield return Tuple.Create(i, _unackedQueue[i]);
        }
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            Head = _preSyncHead;
            
            sd.Set(nameof(Head), Head);
            sd.Set(nameof(Tail), Tail);
            sd.Set(nameof(_unackedQueue), _unackedQueue);
        }

        private static OutgoingMessageQueue Deserialize(IReadOnlyDictionary<string, object> sd)
            => new OutgoingMessageQueue()
            {
                Head = sd.Get<int>(nameof(Head)),
                Tail = sd.Get<int>(nameof(Tail)),
                _unackedQueue = sd.Get<CDictionary<int, ImmutableByteArray>>(nameof(_unackedQueue))
            };
    }
}