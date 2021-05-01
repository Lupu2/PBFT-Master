using System;
using System.Net.Sockets;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.NetworkCommunication
{
    internal class IncomingConnection : IDisposable
    {
        private readonly Socket _socket;
        private readonly IncomingMessageDeliverer _messageDeliverer;

        public IncomingConnection(Socket socket, IncomingMessageDeliverer messageDeliverer)
        {
            _socket = socket;
            _messageDeliverer = messageDeliverer;
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

                        _messageDeliverer.EnqueueForDeliver(messageIndex, buffer);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SendAtIndexToOtherSide(int atIndex) 
            => SafeTry(() => _socket.Send(BitConverter.GetBytes(atIndex)));

        public void Dispose() => SafeTry(_socket.Dispose);
    }
}
