using System;
using System.Net.Sockets;
using Cleipnir.ExecutionEngine;

namespace Cleipnir.NetworkCommunication
{
    internal class IncomingConnection
    {
        private readonly Engine _scheduler;
        private readonly IncomingConnectionInfo _connectionInfo;
        private readonly Socket _socket;
        private readonly Action<ImmutableByteArray> _messageHandler;

        public IncomingConnection(Engine scheduler, IncomingConnectionInfo connectionInfo, Socket socket, Action<ImmutableByteArray> messageHandler)
        {
            _scheduler = scheduler;
            _connectionInfo = connectionInfo;
            _socket = socket;
            _messageHandler = messageHandler;
        }

        public async void Start()
        {
            try
            {
                while (true)
                {
                    var headerBuffer = new byte[8];
                    while (true)
                    {
                        var readSoFar = 0;

                        while (readSoFar < 8)
                            readSoFar += await _socket.ReceiveAsync(
                                new ArraySegment<byte>(headerBuffer, readSoFar, headerBuffer.Length - readSoFar),
                                SocketFlags.None
                            );

                        var messageIndex = BitConverter.ToInt32(headerBuffer);
                        var length = BitConverter.ToInt32(headerBuffer[4..]);

                        readSoFar = 0;
                        var buffer = new byte[length];
                        while (readSoFar < length)
                            readSoFar += await _socket.ReceiveAsync(
                                new ArraySegment<byte>(buffer, readSoFar, length - readSoFar),
                                SocketFlags.None
                            );

                        var messageDeliveryWorkflow = new IncomingMessageDeliveryWorkflow(
                            _connectionInfo,
                            new ImmutableByteArray(buffer), _messageHandler, messageIndex,
                            UpdateAtIndex
                        );

                        _ = _scheduler.Schedule(messageDeliveryWorkflow.Deliver);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void UpdateAtIndex(int atIndex) => _socket.Send(BitConverter.GetBytes(atIndex));
    }
}
