using System;
using System.Collections.Generic;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class ActionSerializer
    {
        public static ActionSerializer<T> Create<T>(long id, Action<T> a) => new ActionSerializer<T>(id, a);
    }

    internal class ActionSerializer<T> : ISerializer
    {
        public long Id { get; }
        public object Instance => Action;

        public Action<T> Action { get; }

        private bool _serialized;

        public ActionSerializer(long id, Action<T> action)
        {
            Id = id;
            Action = action;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; else _serialized = true;

            sd.Set("Target", Action.Target);
            sd.Set("MethodName", Action.Method.Name);
            sd.Set("Parameter", Action.Method.GetParameters()[0].ParameterType.SimpleQualifiedName());
            sd.Set("DeclaringType", Action.Method.DeclaringType?.SimpleQualifiedName());
        }

        private static ActionSerializer<T> Deserialize(long id, IReadOnlyDictionary<string, object> sd)
        {
            var target = sd["Target"];
            var methodName = (string) sd["MethodName"];
            var parameters = new[] { Type.GetType(sd["Parameter"].ToString()) };

            var delegateType = typeof(Action<>).MakeGenericType(parameters);

            var action = target != null
                ? (Action<T>) Delegate.CreateDelegate(delegateType, target, methodName)
                : (Action<T>) Delegate.CreateDelegate(delegateType, Type.GetType((string) sd["DeclaringType"]), methodName);

            return new ActionSerializer<T>(id, action) { _serialized = true };
        }
    }
}
