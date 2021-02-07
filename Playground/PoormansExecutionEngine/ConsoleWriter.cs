using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.PoormansExecutionEngine
{
    public class ConsoleWriter : IPersistable
    {
        private readonly string _toWrite;

        public ConsoleWriter(string toWrite) => _toWrite = toWrite;

        public void Do() => Console.WriteLine(_toWrite);

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(_toWrite), _toWrite);

        private static ConsoleWriter Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ConsoleWriter(sd.Get<string>(nameof(_toWrite)));
    }
}