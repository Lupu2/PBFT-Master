using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public static class PAction
    {
        public static PAction<T> Create<T>(T parameter, Action<T> method) 
            => new PAction<T>(method, parameter);

        public static PAction<T1, T2> Create<T1, T2>(T1 parameter1, T2 parameter2, Action<T1, T2> method)
            => new PAction<T1, T2>(method, parameter1, parameter2);
    }

    public class PAction<T> : IPersistable
    {
        private readonly T _parameter1;
        private readonly Action<T> _method;

        private bool _serialized;

        public PAction(Action<T> method, T parameter1)
        {
            _method = method;
            _parameter1 = parameter1;
        }

        public void Invoke() => _method(_parameter1);

        public Action Action => Invoke;

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; _serialized = true;

            sd.Set(nameof(_parameter1), _parameter1);
            sd.Set(nameof(_method), _method);
        }

        private static PAction<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new PAction<T>(
                sd.Get<Action<T>>(nameof(_method)),
                sd.Get<T>(nameof(_parameter1))
            ) { _serialized = true };
        }

        public static implicit operator Action(PAction<T> a) => a.Action;

    }

    public class PAction<T1, T2> : IPersistable
    {
        private readonly T1 _parameter1;
        private readonly T2 _parameter2;
        private readonly Action<T1, T2> _method;

        private bool _serialized;

        public PAction(Action<T1, T2> method, T1 parameter1, T2 parameter2)
        {
            _method = method;
            _parameter1 = parameter1;
            _parameter2 = parameter2;
        }

        public void Invoke() => _method(_parameter1, _parameter2);

        public Action Action => Invoke;

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; _serialized = true;

            sd.Set(nameof(_parameter1), _parameter1);
            sd.Set(nameof(_parameter2), _parameter2);
            sd.Set(nameof(_method), _method);
        }

        private static PAction<T1, T2> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new PAction<T1, T2>(
                    sd.Get<Action<T1, T2>>(nameof(_method)),
                    sd.Get<T1>(nameof(_parameter1)),
                    sd.Get<T2>(nameof(_parameter2))
                ) { _serialized = true };
        }

        public static implicit operator Action(PAction<T1, T2> a) => a.Action;
    }
}
