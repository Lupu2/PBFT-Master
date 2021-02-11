using System;
using System.Collections.Generic;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    public class DelegateSerializer : ISerializer
    {
        public long Id { get; }
        public object Instance => _delegate;

        private readonly Delegate _delegate;

        private bool _serialized;

        public DelegateSerializer(long id, Delegate @delegate)
        {
            Id = id;
            _delegate = @delegate;
        }

        #region Equality

        protected bool Equals(DelegateSerializer other) => Id.Equals(other.Id) && Equals(Instance, other.Instance);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DelegateSerializer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ (Instance != null ? Instance.GetHashCode() : 0);
            }
        }
        #endregion

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; else _serialized = true;

            sd.Set("DelegateType", _delegate.GetType().SimpleQualifiedName());
            sd.Set("Target", _delegate.Target);
            sd.Set("MethodName", _delegate.Method.Name);
            sd.Set("DeclaringType", _delegate.Method.DeclaringType?.SimpleQualifiedName());
        }

        public static DelegateSerializer Deserialize(long id, IReadOnlyDictionary<string, object> serializedState)
        {
            var delegateType = Type.GetType((string) serializedState["DelegateType"]);
            var target = serializedState["Target"];
            var methodName = (string) serializedState["MethodName"];

            if (target != null)
                return new DelegateSerializer(id, Delegate.CreateDelegate(delegateType, target, methodName));

            var declaringType = Type.GetType((string) serializedState["DeclaringType"]);

            return new DelegateSerializer(id, Delegate.CreateDelegate(delegateType, declaringType, methodName)) { _serialized = true };
        }
    }
}
