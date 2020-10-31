using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.NetworkCommunication
{
    internal class IncomingConnectionInfo : IPersistable
    {
        public IncomingConnectionInfo(PointToPointMessageQueue queue) => Queue = queue;

        public int AtIndex { get; set; }
        public PointToPointMessageQueue Queue { get; }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(AtIndex), AtIndex);
            sd.Set(nameof(Queue), Queue);
        }

        private static IncomingConnectionInfo Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new IncomingConnectionInfo(sd.Get<PointToPointMessageQueue>(nameof(Queue)))
                { AtIndex = sd.Get<int>(nameof(AtIndex)) };
        }
    }
}