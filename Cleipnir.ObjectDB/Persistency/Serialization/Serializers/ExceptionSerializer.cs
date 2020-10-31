using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Newtonsoft.Json;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class ExceptionSerializer : ISerializer
    {
        public long Id { get; }
        public object Instance => _exception;

        private readonly Exception _exception;

        private bool _serialized;

        public ExceptionSerializer(long id, Exception exception)
        {
            Id = id;
            _exception = exception;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return;
            _serialized = true;

            sd.Set(nameof(_exception), JsonConvert.SerializeObject(_exception));
            sd.Set("ExceptionType", _exception.GetType().SimpleQualifiedName());
        }

        private static ExceptionSerializer Deserialize(long id, IReadOnlyDictionary<string, object> sd)
        {
            var exceptionType = Type.GetType(sd.Get<string>("ExceptionType"));
            var exception = (Exception) JsonConvert.DeserializeObject(sd.Get<string>(nameof(_exception)), exceptionType);

            return new ExceptionSerializer(id, exception) { _serialized = true };
        }
    }
}
