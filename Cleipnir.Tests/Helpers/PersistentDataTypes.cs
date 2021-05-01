using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Tests.Helpers
{
    public class PValue<T> : IPersistable
    {
        public T Value { get; set; }

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(Value), Value);

        private static PValue<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new PValue<T> { Value = sd.Get<T>(nameof(Value)) };
    }

    public class PTuple<T1, T2> : IPersistable
    {
        public PTuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }

        public T1 First { get; }
        public T2 Second { get; }
        private bool _serialized;


        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; _serialized = true;

            sd.Set(nameof(First), First);
            sd.Set(nameof(Second), Second);
        }

        private static PTuple<T1, T2> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new PTuple<T1, T2>(
                    sd.Get<T1>(nameof(First)),
                    sd.Get<T2>(nameof(Second)))
                { _serialized = true };
    }
}
