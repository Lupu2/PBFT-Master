using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class PersistableSerializer : ISerializer
    {
        public long Id { get; }
        public object Instance => _persistable;
        private readonly IPersistable _persistable;

        //private readonly StateMap _persistableStateMap;

        public PersistableSerializer(long id, IPersistable persistable)
        {
            Id = id;
            _persistable = persistable;
            //_persistableStateMap = sm;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set("Type", _persistable.GetType().SimpleQualifiedName());

            _persistable.Serialize(sd, helper);
            //stateToSerialize["PersistableStateMap"] = _persistableStateMap;
        }

        public static PersistableSerializer Deserialize(long id, IReadOnlyDictionary<string, object> sm, ISet<object> instances)
        {
            if (!sm.ContainsKey("Type"))
                Console.WriteLine("OH NO");
            
            var type = Type.GetType(sm["Type"].ToString());
            //var persistableStateMap = (StateMap) sm["PersistableStateMap"];

            var instance = (IPersistable) DeserializationMethodInvoker.Invoke(
                id,
                type,
                sm.ToDictionary(kv => kv.Key, kv => kv.Value),
                instances
            );

            return new PersistableSerializer(id, instance);
        }
    }
}
