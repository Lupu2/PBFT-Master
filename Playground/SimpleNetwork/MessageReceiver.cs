using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Playground.SimpleNetwork
{
    public class MessageReceiver : IDisposable
    {
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly IPEndPoint _endPoint;
        private readonly Action<byte[]> _messageHandler;

        private volatile bool _disposed;

        public MessageReceiver(IPEndPoint endPoint, Action<byte[]> messageHandler)
        {
            _endPoint = endPoint;
            _messageHandler = messageHandler;
        }

        public void StartServing() => new Thread(Serve){IsBackground = true}.Start();

        private async void Serve()
        {
            try
            {
                _socket.Bind(_endPoint);
                _socket.Listen(10);

                while (!_disposed)
                {
                    var socket = await _socket.AcceptAsync();
                    _ = HandleNewConnection(socket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        private async Task HandleNewConnection(Socket socket)
        {
            while (!_disposed)
            {
                var received = 0;
                var lengthBuffer = new byte[4];
                while (received < 4)
                    received += await socket.ReceiveAsync(
                        new ArraySegment<byte>(lengthBuffer, received, lengthBuffer.Length - received),
                        SocketFlags.None
                    );

                var length = BitConverter.ToInt32(lengthBuffer);
                received = 0;
                var messageBuffer = new byte[length];
                while (received < length)
                    received += await socket.ReceiveAsync(
                        new ArraySegment<byte>(messageBuffer, received, messageBuffer.Length - received),
                        SocketFlags.None
                    );
                
                _messageHandler(messageBuffer);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            try
            {
                _socket.Dispose();
            } catch {}
        }
    }
}
