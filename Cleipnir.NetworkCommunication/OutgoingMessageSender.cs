using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.NetworkCommunication
{
    internal class OutgoingMessageSender
    {
        private readonly IPEndPoint _endPoint;
        private readonly Guid _identifier;
        private readonly Action<int> _ackUpUntil;
        private readonly Action _onLostConnection;
        private Socket _socket;

        private readonly TaskCompletionSource<bool> _connected = new TaskCompletionSource<bool>();
        private int _atIndex;
        private readonly Engine _scheduler;

        public OutgoingMessageSender(
            IPEndPoint endPoint, Guid identifier, 
            int atIndex,
            Action<int> ackUpUntil,
            Action onLostConnection)
        {
            _endPoint = endPoint;
            _identifier = identifier;
            _atIndex = atIndex;
            _scheduler = Engine.Current;
            _ackUpUntil = ackUpUntil;
            _onLostConnection = onLostConnection;
        }

        public async void CreateConnection()
        {
            while (true)
            {
                try
                {
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await _socket.ConnectAsync(_endPoint);

                    await _socket.SendAsync(_identifier.ToByteArray(), SocketFlags.None);

                    Receiver(_socket);

                    _connected.SetResult(true);

                    return;
                }
                catch (Exception _)
                {
                    await Task.Delay(1000);
                }
            }
        }

        public async Task Send(byte[] msg)
        {
            await _connected.Task;
            try
            {
                var arraySegments = new List<ArraySegment<byte>>();

                arraySegments.Add(BitConverter.GetBytes(_atIndex++));
                arraySegments.Add(BitConverter.GetBytes(msg.Length));
                arraySegments.Add(msg);

                await _socket.SendAsync(arraySegments, SocketFlags.None);
            }
            catch (Exception exception)
            {
                SafeTry(_socket.Dispose);
                _ = _scheduler.Schedule(_onLostConnection);
                throw;
            }
        }

        private async void Receiver(Socket socket)
        {
            var ackedUntilBuffer = new byte[4];
            while (true)
            {
                var readSoFar = 0;

                while (readSoFar < 4)
                    readSoFar += await socket.ReceiveAsync(
                        new ArraySegment<byte>(ackedUntilBuffer, readSoFar, ackedUntilBuffer.Length - readSoFar),
                        SocketFlags.None
                    );

                var ackedUntil = BitConverter.ToInt32(ackedUntilBuffer);

                _ = _scheduler.Schedule(() => _ackUpUntil(ackedUntil));
            }
        }
    }
}
