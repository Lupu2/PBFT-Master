using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.NetworkCommunication
{
    public class ConnectionServer : IPersistable
    {
        private readonly IncomingCoordinator _coordinator;

        public ConnectionServer(string hostname, int port, Action<ImmutableByteArray> messageHandler) 
            => _coordinator = new IncomingCoordinator(hostname, port, messageHandler);

        private ConnectionServer(IncomingCoordinator coordinator) => _coordinator = coordinator;

        public void Shutdown() => _coordinator.Shutdown();

        public void Serialize(StateMap sd, SerializationHelper helper) => sd.Set(nameof(_coordinator), _coordinator);

        private static ConnectionServer Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ConnectionServer(sd.Get<IncomingCoordinator>(nameof(_coordinator)));
    }
}
