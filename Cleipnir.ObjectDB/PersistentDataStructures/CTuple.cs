using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public static class CTuple
    {
        public static CTuple<T1, T2> Create<T1, T2>(T1 first, T2 second)
            => new CTuple<T1, T2>(first, second);

        public static CTuple<T1, T2, T3> Create<T1, T2, T3>(T1 first, T2 second, T3 third)
            => new CTuple<T1, T2, T3>(first, second, third);
    }

    public class CTuple<T1, T2> : IPersistable
    {
        public CTuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }

        public T1 First { get; }
        public T2 Second { get; }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(First), First);
            sd.Set(nameof(Second), Second);
        }

        private static CTuple<T1, T2> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CTuple<T1, T2>(
                sd.Get<T1>(nameof(First)),
                sd.Get<T2>(nameof(Second))
            );
    }

    public class CTuple<T1, T2, T3> : IPersistable
    {
        public CTuple(T1 first, T2 second, T3 third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public T1 First { get; }
        public T2 Second { get; }
        public T3 Third { get; }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(First), First);
            sd.Set(nameof(Second), Second);
            sd.Set(nameof(Third), Third);
        }

        private static CTuple<T1, T2, T3> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CTuple<T1, T2, T3>(
                sd.Get<T1>(nameof(First)),
                sd.Get<T2>(nameof(Second)),
                sd.Get<T3>(nameof(Third))
            );
    }
}
