using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Rx;

namespace Cleipnir.Tests.ReactiveTests
{
    internal class ValueEmitter : IPersistable
    {
        private readonly ValueHolder _valueHolder;

        public ValueEmitter(Stream<int> s, ValueHolder valueHolder)
        {
            _valueHolder = valueHolder;
            s.CallOnEvent(AddValue);
        }

        public ValueEmitter(ValueHolder valueHolder) => _valueHolder = valueHolder;

        private void AddValue(int next) => _valueHolder.Value = next;

        public void Serialize(StateMap sd, SerializationHelper helper) 
            => sd.Set(nameof(_valueHolder), _valueHolder);

        private static ValueEmitter Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ValueEmitter(sd.Get<ValueHolder>(nameof(_valueHolder)));
    }
}
