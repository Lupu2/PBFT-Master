using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Rx.ExecutionEngine
{
    internal class HandlerAndEvent<T> : IPersistable
    {
        private readonly Action<T> _handler;
        private readonly T _value;

        public HandlerAndEvent(Action<T> handler, T value)
        {
            _handler = handler;
            _value = value;
        }

        public void Deliver() => _handler(_value);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(_handler), _handler);
            sd.Set(nameof(_value), _value);
        }

        private static HandlerAndEvent<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new HandlerAndEvent<T>(
                sd.Get<Action<T>>(nameof(_handler)),
                sd.Get<T>(nameof(_value))
            );
    }
}
