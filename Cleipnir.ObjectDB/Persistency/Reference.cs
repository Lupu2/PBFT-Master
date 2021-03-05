using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Helpers.FunctionalTypes;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Persistency.Persistency
{
    public class Reference
    {
        public Option<object> Value { get; } = Option.None<object>();
        public long? Id { get; }

        public ISerializer Serializer { get; private set; }

        public bool IsReferencing => Id.HasValue;

        private Action<object> _onValue;

        private Reference(long id) => Id = id;

        internal Reference(object value) => Value = Option.Some(value);

        internal Reference(ISerializer wp)
        {
            Id = wp.Id;
            Serializer = wp;
        }

        public void DoWhenResolved(Action<object> action)
        {
            if (Value.HasValue)
                action(Value.GetValue);
            else if (Serializer != null)
                action(Serializer.Instance);
            else
                _onValue = action;
        }

        public void DoWhenResolved<T>(Action<T> action) => DoWhenResolved(o => action((T) o));

        internal void SetSerializer(ISerializer serializer)
        {
            Serializer = serializer;
            _onValue?.Invoke(serializer.Instance);
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            var value = Value.HasValue ? Value.GetValue : null;
            sd.Set("Value", value);
            sd.Set("Id", Id);
        }

        public static Reference Deserialize(IReadOnlyDictionary<string, object> sm)
        {
            if (sm["Id"] == null)
                return new Reference(sm["Value"]);
            else 
                return new Reference((long) sm["Id"]);
        }

        public override bool Equals(object other)
        {
            if (!(other is Reference otherRef))
                return false;

            if (Id.HasValue && otherRef.Id.HasValue)
                return Id.Value == otherRef.Id.Value;
            else if (!Id.HasValue && !otherRef.Id.HasValue)
            {
                if (Value.GetValue == null && otherRef.Value == null) return false;
                if (Value.GetValue == null || otherRef.Value == null) return true;

                return Value.GetValue.Equals(otherRef.Value.GetValue);
            }
            
            return false;
        }

        public override int GetHashCode()
        {
            if (Id.HasValue)
                return Id.Value.GetHashCode();
            else
                return Value.GetValue.GetHashCode();
        }
    }
}
