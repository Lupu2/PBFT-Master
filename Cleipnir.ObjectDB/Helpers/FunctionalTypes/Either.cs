using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.Helpers.FunctionalTypes
{
    public class Either<T1, T2> : IPersistable
    {
        private readonly T1 _t1;
        private readonly bool _t1HasValue;

        private readonly T2 _t2;

        private bool _serialized;

        public Either(T1 t1)
        {
            _t1 = t1;
            _t1HasValue = true;
        }

        public Either(T2 t2) => _t2 = t2;

        public TOut Match<TOut>(Func<T1, TOut> matchFirst, Func<T2, TOut> matchSecond)
            => _t1HasValue ? matchFirst(_t1) : matchSecond(_t2);

        public bool Is<T>() 
        {
            if (typeof(T) != typeof(T1) && typeof(T) != typeof(T2))
                throw new ArgumentException($"TypeName must be of either type {typeof(T1).Name} or {typeof(T2).Name} not none or both");

            return _t1HasValue ? typeof(T) == typeof(T1) : typeof(T) == typeof(T2);
        }

        public bool HasFirst => _t1HasValue;
        public bool HasSecond => !HasFirst;

        public T1 First => _t1HasValue ? _t1 : throw new InvalidOperationException();
        public T2 Second => !_t1HasValue ? _t2 : throw new InvalidOperationException();

        public static Either<T1, T2> Create(T1 t1) => new Either<T1, T2>(t1);

        public static Either<T1, T2> Create(T2 t2) => new Either<T1, T2>(t2);
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return;
            _serialized = true;

            sd.Set(nameof(HasFirst), HasFirst);
            sd.Set(nameof(First), First);
            sd.Set(nameof(Second), Second);
        }

        private static Either<T1, T2> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var hasFirst = sd.Get<bool>(nameof(HasFirst));
            return hasFirst
                ? new Either<T1, T2>(sd.Get<T1>(nameof(First)))
                : new Either<T1, T2>(sd.Get<T2>(nameof(Second)));
        }
    }
}
