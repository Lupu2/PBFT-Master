using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Tests.ReactiveTests
{
    internal class ValueHolder : IPersistable
    {
        public int Value { get; set; }

        public void SetValue(int value) => Value = value;

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(Value), Value);

        private static ValueHolder Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ValueHolder { Value = sd.Get<int>(nameof(Value)) };
    }

    internal class ValueHolder<T> : IPersistable
    {
        public T Value { get; set; }

        public void SetValue(T value) => Value = value;

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(Value), Value);

        private static ValueHolder<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ValueHolder<T> { Value = sd.Get<T>(nameof(Value)) };
    }
}
