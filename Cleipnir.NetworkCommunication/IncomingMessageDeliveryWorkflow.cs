using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.NetworkCommunication
{
    internal class IncomingMessageDeliveryWorkflow : IPersistable
    {
        private readonly IncomingConnectionInfo _connectionInfo;
        private readonly Action<ImmutableByteArray> _messageHandler;
        private readonly int _messageIndex;
        private readonly ImmutableByteArray _message;
        public Action<int> UpdateAtIndex { get; set; }

        private readonly PointToPointMessageQueue _toDeliver;

        private bool _serialized;

        public IncomingMessageDeliveryWorkflow(
            IncomingConnectionInfo connectionInfo, 
            ImmutableByteArray message, Action<ImmutableByteArray> messageHandler, int messageIndex, 
            Action<int> updateAtIndex)
        {
            _connectionInfo = connectionInfo;
            _message = message;
            _messageHandler = messageHandler;
            _messageIndex = messageIndex;
            UpdateAtIndex = updateAtIndex;
            _toDeliver = _connectionInfo.Queue;
        }

        public void Deliver()
        {
            if (_messageIndex < _connectionInfo.AtIndex || _toDeliver.ContainsKey(_messageIndex))
                return;

            Sync.AfterNext(AfterSync, true);
        }

        private void AfterSync()
        {
            if (_messageIndex < _connectionInfo.AtIndex)
                return;

            _toDeliver[_messageIndex] = _message;

            if (!_toDeliver.ContainsKey(_messageIndex)) return;

            while (_toDeliver.ContainsKey(_connectionInfo.AtIndex))
            {
                _messageHandler(_toDeliver[_messageIndex]);
                _toDeliver.Remove(_connectionInfo.AtIndex);
                _connectionInfo.AtIndex++;
            }
            
            if (UpdateAtIndex != null) 
                Task.Run(() => UpdateAtIndex(_connectionInfo.AtIndex));
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; _serialized = true;

            sd.Set(nameof(_connectionInfo), _connectionInfo);
            sd.Set(nameof(_message), _message);
            sd.Set(nameof(_messageHandler), _messageHandler);
            sd.Set(nameof(_messageIndex), _messageIndex);
        }

        private static IncomingMessageDeliveryWorkflow Deserialize(IReadOnlyDictionary<string, object> sd)
            => new IncomingMessageDeliveryWorkflow(
                sd.Get<IncomingConnectionInfo>(nameof(_connectionInfo)),
                sd.Get<ImmutableByteArray>(nameof(_message)),
                sd.Get<Action<ImmutableByteArray>>(nameof(_messageHandler)),
                sd.Get<int>(nameof(_messageIndex)),
                null
            ) {_serialized = true};
    }
}