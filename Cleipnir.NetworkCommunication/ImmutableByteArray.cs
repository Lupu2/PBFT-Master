using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.NetworkCommunication
{
    public class ImmutableByteArray : IPersistable
    {
        public byte[] Array { get; }

        public ImmutableByteArray(byte[] array) => Array = array;

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(Array), System.Convert.ToBase64String(Array));

        private static ImmutableByteArray Deserialize(IReadOnlyDictionary<string, object> sd) 
            => new ImmutableByteArray(Convert.FromBase64String(sd.Get<string>(nameof(Array))));
    }
}
