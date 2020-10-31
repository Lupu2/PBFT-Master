using System;
using System.Net;
using System.Net.Sockets;
using Cleipnir.ExecutionEngine;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.NetworkCommunication
{
    internal class ConnectionListener : IDisposable
    {
        private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly IPEndPoint _endPoint;
        private readonly Action<Guid, Socket> _onNewConnection;
        private readonly Engine _scheduler;

        private volatile bool _disposed;

        public ConnectionListener(IPEndPoint endPoint, Action<Guid, Socket> onNewConnection, Engine scheduler)
        {
            _endPoint = endPoint;
            _onNewConnection = onNewConnection;
            _scheduler = scheduler;
        }

        public async void StartServing()
        {
            try
            {
                _socket.Bind(_endPoint);
                _socket.Listen(10);
                var nodeIdBytes = new byte[16];

                while (!_disposed)
                {
                    var socket = await _socket.AcceptAsync();
                    //socket.ReceiveBufferSize = 8192;
                    var received = 0;
                    while (received < 16)
                        received += socket.Receive(nodeIdBytes, received, 16 - received, SocketFlags.None);

                    var identifier = new Guid(nodeIdBytes);

                    _ = _scheduler.Schedule(() => _onNewConnection(identifier, socket));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            SafeTry(_socket.Dispose);
        }
    }
}
