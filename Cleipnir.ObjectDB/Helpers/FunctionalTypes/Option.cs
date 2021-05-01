using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.Helpers.FunctionalTypes
{
    public static class Option
    {
        public static Option<T> Some<T>(T value) => new Some<T>(value);
        public static Option<T> None<T>() => new None<T>();
    }

    public abstract class Option<T> : IPersistable
    {
        public abstract TOut Match<TOut>(Func<T, TOut> withValue, TOut withoutValue);

        public abstract void Match(Action<T> withValue, Action withoutValue);

        public abstract T GetOr(T fallback);

        public abstract T GetValue { get; }

        public abstract Option<T> GetOr(Option<T> fallback);

        public abstract bool HasValue { get; }

        public abstract void Serialize(StateMap stateToSerialize, SerializationHelper helper);
    }

    public static class Some
    {
        public static Some<T> Create<T>(T t) => new Some<T>(t);
    }

    public class Some<T> : Option<T>
    {
        public Some(T value) => GetValue = value;

        public override TOut Match<TOut>(Func<T, TOut> withValue, TOut withoutValue)
            => withValue(GetValue);

        public override void Match(Action<T> withValue, Action withoutValue)
            => withValue(GetValue);

        public override T GetOr(T fallback) => GetValue;
        public override T GetValue { get; } 

        public override Option<T> GetOr(Option<T> fallback) => this;
        public override bool HasValue { get; } = true;

        public override void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(GetValue), GetValue);

        internal static Some<T> Deserialize(IReadOnlyDictionary<string, object> sd) => new Some<T>((T) sd[nameof(GetValue)]);
    }

    public class None<T> : Option<T>
    {
        public override TOut Match<TOut>(Func<T, TOut> withValue, TOut withoutValue)
            => withoutValue;

        public override void Match(Action<T> withValue, Action withoutValue)
            => withoutValue();

        public override T GetOr(T fallback)
            => fallback;

        public override T GetValue => throw new InvalidOperationException("Option does not contain any value");

        public override Option<T> GetOr(Option<T> fallback)
            => fallback;

        public override bool HasValue { get; } = false;
        
        public override void Serialize(StateMap stateToSerialize, SerializationHelper helper) { }
        internal static None<T> Deserialize() => new None<T>();
    }
}
