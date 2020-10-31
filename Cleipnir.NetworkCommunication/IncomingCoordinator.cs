using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.NetworkCommunication
{
    internal class IncomingCoordinator : IPersistable
    {
        private readonly CDictionary<Guid, IncomingConnectionInfo> _connectionInfos;
        private readonly string _hostName;
        private readonly int _port;
        private readonly Action<ImmutableByteArray> _messageHandler;

        private readonly Dictionary<Guid, Socket> _sockets = new Dictionary<Guid, Socket>();

        private readonly Engine _scheduler;

        private bool _synchronized;
        private readonly ConnectionListener _connectionListener;

        public IncomingCoordinator(
            string hostName, int port, Action<ImmutableByteArray> messageHandler,
            CDictionary<Guid, IncomingConnectionInfo> connectionInfos = null)
        {
            _hostName = hostName;
            _port = port;
            _messageHandler = messageHandler;
            _scheduler = Engine.Current;

            _connectionInfos = connectionInfos ?? new CDictionary<Guid, IncomingConnectionInfo>();

            _connectionListener = new ConnectionListener(IPEndPoint.Parse($"{hostName}:{port}"), HandleNewConnection, _scheduler);
            _scheduler.Schedule(_connectionListener.StartServing);
        }

        public void Shutdown()
        {
            _sockets.Values.ForEach(s => SafeTry(s.Dispose));
            _connectionListener.Dispose();
        } 

        private void HandleNewConnection(Guid id, Socket socket)
        {
            if (!_connectionInfos.ContainsKey(id))
                _connectionInfos[id] = new IncomingConnectionInfo(new PointToPointMessageQueue(new CDictionary<int, ImmutableByteArray>()));

            var connectionInfo = _connectionInfos[id];
            var connection = new IncomingConnection(_scheduler, connectionInfo, socket, _messageHandler);
            connection.Start();
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_synchronized) return; _synchronized = true;

            sd.Set(nameof(_hostName), _hostName);
            sd.Set(nameof(_port), _port);
            sd.Set(nameof(_messageHandler), _messageHandler);
            sd.Set(nameof(_connectionInfos), _connectionInfos);
        }

        private static IncomingCoordinator Deserialize(IReadOnlyDictionary<string, object> sd)
            => new IncomingCoordinator(
                sd.Get<string>(nameof(_hostName)),
                sd.Get<int>(nameof(_port)),
                sd.Get<Action<ImmutableByteArray>>(nameof(_messageHandler)),
                sd.Get<CDictionary<Guid, IncomingConnectionInfo>>(nameof(_connectionInfos))
            ) {_synchronized = true};
    }
}
