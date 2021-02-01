using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cleipnir.ExecutionEngine;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.NetworkCommunication
{
    internal class ConnectionListener : IDisposable
    {
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly IPEndPoint _endPoint;
        private readonly Action<string, Socket> _onNewConnection;
        private readonly Engine _scheduler;

        private volatile bool _disposed;

        public ConnectionListener(IPEndPoint endPoint, Action<string, Socket> onNewConnection, Engine scheduler)
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
                    
                    if (_disposed)
                        return;
                    
                    //socket.ReceiveBufferSize = 8192;
                    var received = 0;

                    var headerBuffer = new byte[4];
                    while (received < 4)
                        received += await _socket.ReceiveAsync(
                            new ArraySegment<byte>(headerBuffer, received, headerBuffer.Length - received),
                            SocketFlags.None
                        );
                    
                    var identifierLength = BitConverter.ToInt32(headerBuffer);
                    var identifierBuffer = new byte[identifierLength];
                    
                    received = 0;
                    while (received < identifierBuffer.Length)
                        received += socket.Receive(nodeIdBytes, received, identifierLength - received, SocketFlags.None);

                    var identifier = Encoding.UTF8.GetString(identifierBuffer);

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
