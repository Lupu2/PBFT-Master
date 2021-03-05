using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine
{
    [AsyncMethodBuilder(typeof(CMethodBuilder))]
    public class CTask : IPersistable
    {
        public CTask() => Awaitable = new CAwaitable();

        private CTask(CAwaitable awaitable) =>Awaitable = awaitable;

        public CAwaitable.Awaiter GetAwaiter() => Awaitable.GetAwaiter();
        public CAwaitable Awaitable { get; }

        internal void SignalCompletion() => Awaitable.SignalCompletion();

        internal void SignalThrownException(Exception e) => Awaitable.SignalThrownException(e);

        #region Persisting

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(Awaitable), Awaitable);

        internal static CTask Deserialize(IReadOnlyDictionary<string, object> sd) 
            => new CTask((CAwaitable) sd[nameof(Awaitable)]);
        #endregion
    }

    [AsyncMethodBuilder(typeof(CorumsMethodBuilder<>))]
    public class CTask<T> : IPersistable
    {
        public CTask() => Awaitable = new CAwaitable<T>();
        private CTask(CAwaitable<T> awaitable) => Awaitable = awaitable;

        internal void SignalCompletion(T result) => Awaitable.SignalCompletion(result);

        internal void SignalThrownException(Exception e) => Awaitable.SignalThrownException(e);

        public CAwaitable<T>.Awaiter GetAwaiter() => Awaitable.GetAwaiter();
        public CAwaitable<T> Awaitable { get; }

        public void Serialize(StateMap sd, SerializationHelper helper) 
            => sd.Set(nameof(Awaitable), Awaitable);

        internal static CTask<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CTask<T>((CAwaitable<T>) sd[nameof(Awaitable)]);
    }
}