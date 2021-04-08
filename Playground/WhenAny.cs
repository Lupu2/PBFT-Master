using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground
{
    public class WhenAny<T> : IPersistable
    {
        public CAwaitable<T> Awaitable { get; }

        public WhenAny(IEnumerable<CTask<T>> tasks)
        {
            Awaitable = new CAwaitable<T>();

            foreach (var task in tasks)
            {
                var awaiter = task.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    if (Awaitable.Completed) return;
                    if (awaiter.ThrownException.HasValue)
                        Awaitable.SignalThrownException(awaiter.ThrownException.GetValue);
                    else
                        Awaitable.SignalCompletion(awaiter.GetResult());
                });
            }
        }

        private WhenAny(CAwaitable<T> awaitable) => Awaitable = awaitable;

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd[nameof(Awaitable)] = Awaitable;
        }

        private static WhenAny<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new WhenAny<T>(sd.Get<CAwaitable<T>>(nameof(Awaitable)));
        }

        public static CAwaitable<T> Of(params CTask<T>[] tasks) => new WhenAny<T>(tasks).Awaitable;
    }
}