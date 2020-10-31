using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace Cleipnir.NetworkCommunication
{
    public class OutgoingConnection : IPersistable
    {
        private readonly string _hostName;
        private readonly int _port;
        private readonly Guid _identifier;
        private readonly IPEndPoint _endPoint;

        private readonly CQueue<ImmutableByteArray> _unackedQueue;
        private int _ackedUntil;

        private readonly QueueWorker _queueWorker = new QueueWorker();
        private OutgoingMessageSender _messageSender;

        public OutgoingConnection(string hostName, int port)
        {
            _hostName = hostName;
            _port = port;
            _endPoint = IPEndPoint.Parse($"{hostName}:{port}");
            _unackedQueue = new CQueue<ImmutableByteArray>();
            _ackedUntil = -1;
            _identifier = Guid.NewGuid();

            Scheduler.Schedule(CreateNewMessageSender, false);
        }

        private OutgoingConnection(
            string hostName, int port, Guid identifier, 
            CQueue<ImmutableByteArray> unackedQueue, int ackedUntil)
        {
            _hostName = hostName;
            _port = port;
            _unackedQueue = unackedQueue;
            _ackedUntil = ackedUntil;
            _identifier = identifier;
            
            Scheduler.Schedule(CreateNewMessageSender, false);
        }

        public void Send(byte[] msg)
        {
            _unackedQueue.Enqueue(new ImmutableByteArray(msg));
            var messageSender = _messageSender;
            Task SendMsg() => messageSender?.Send(msg) ?? Task.CompletedTask;
            Sync.AfterNext(() => _queueWorker.Do(SendMsg), false);
        }

        private void CreateNewMessageSender()
        {
            _messageSender = new OutgoingMessageSender(
                _endPoint,
                _identifier,
                _ackedUntil + 1,
                HandleAck,
                HandleLostConnection
            );

            _messageSender.CreateConnection();

            foreach (var bytes in _unackedQueue)
                _queueWorker.Do(() => _messageSender.Send(bytes.Array));
        }

        private void HandleAck(int ackedUntil)
        {
            while (_ackedUntil < ackedUntil)
            {
                _unackedQueue.Dequeue();
                _ackedUntil++;
            }
        }

        private void HandleLostConnection() => CreateNewMessageSender();

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(_hostName), _hostName);
            sd.Set(nameof(_port), _port);
            sd.Set(nameof(_identifier), _identifier);
            sd.Set(nameof(_unackedQueue), _unackedQueue);
            sd.Set(nameof(_ackedUntil), _ackedUntil);
        }

        private static OutgoingConnection Deserialize(IReadOnlyDictionary<string, object> sd)
            => new OutgoingConnection(
                sd.Get<string>(nameof(_hostName)),
                sd.Get<int>(nameof(_port)),
                sd.Get<Guid>(nameof(_identifier)),
                sd.Get<CQueue<ImmutableByteArray>>(nameof(_unackedQueue)),
                sd.Get<int>(nameof(_ackedUntil))
            );
    }
}
