using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.NetworkCommunication
{
    internal class OutgoingConnection : IDisposable
    {
        private readonly IPEndPoint _endPoint;
        private readonly string _identifier;
        private Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        private readonly OutgoingMessageQueue _persistentUnackedMessageQueue;
        private Queue<Tuple<int, ImmutableByteArray>> _localQueue;

        private bool _running;
        
        private readonly Engine _engine;

        private volatile bool _disposed;

        private readonly object _sync = new();

        public OutgoingConnection(IPEndPoint endPoint, string identifier, OutgoingMessageQueue persistentUnackedMessageQueue)
        {
            _endPoint = endPoint;
            _identifier = identifier;
            _persistentUnackedMessageQueue = persistentUnackedMessageQueue;

            _localQueue = new (persistentUnackedMessageQueue.GetSyncedUnackedMessages());
            
            _engine = Engine.Current;
        }

        public void Start() => Notify();

        public void Send(int index, ImmutableByteArray bytes)
        {
            lock (_sync)
                _localQueue.Enqueue(Tuple.Create(index, bytes));
            
            Notify();
        }

        private void Notify()
        {
            lock (_sync)
            {
                if (_running || _disposed)
                    return;

                _running = true;
            }

            _ = ExecuteSendMessagesWorkflow();
        }

        private async Task ExecuteSendMessagesWorkflow()
        {
            while (!_disposed)
            {
                int atIndex;
                ImmutableByteArray payload;
                
                lock (_sync)
                    if (_localQueue.Count == 0)
                    {
                        _running = false;
                        return;
                    }
                    else
                        (atIndex, payload) = _localQueue.Dequeue();
                try
                {
                    if (_socket == null || !_socket.Connected)    
                        await CreateConnection();
                    
                    await SendMessage(atIndex, payload.Array);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Restart();
                }
            }
        }

        public async Task CreateConnection()
        {
            while (true)
            {
                try
                {
                    SafeTry(_socket.Dispose);
                    
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await _socket.ConnectAsync(_endPoint);

                    var identifierBytes = Encoding.UTF8.GetBytes(_identifier);

                    await _socket.SendAsync(BitConverter.GetBytes(identifierBytes.Length), SocketFlags.None);
                    await _socket.SendAsync(identifierBytes, SocketFlags.None);

                    Receiver(_socket);

                    return;
                }
                catch (Exception _)
                {
                    Console.WriteLine($"Unable to connect to endpoint: {_endPoint} retrying in 5 seconds");
                    await Task.Delay(5000);
                }
            }
        }
        
        private async Task SendMessage(int atIndex, byte[] payload)
        {
            var arraySegments = new List<ArraySegment<byte>>();

            arraySegments.Add(BitConverter.GetBytes(atIndex));
            arraySegments.Add(BitConverter.GetBytes(payload.Length));
            arraySegments.Add(payload);

            await _socket.SendAsync(arraySegments, SocketFlags.None);
        }
        
        private async void Receiver(Socket socket)
        {
            var ackedUntilBuffer = new byte[4];
            try
            {
                while (!_disposed)
                {
                    var readSoFar = 0;

                    while (readSoFar < 4)
                        readSoFar += await socket.ReceiveAsync(
                            new ArraySegment<byte>(ackedUntilBuffer, readSoFar, ackedUntilBuffer.Length - readSoFar),
                            SocketFlags.None
                        );

                    var ackedUntil = BitConverter.ToInt32(ackedUntilBuffer);

                    _ = _engine.Schedule(() => _persistentUnackedMessageQueue.AckUntil(ackedUntil));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Restart();
            }
        }

        private void Restart()
        {
            var unackedMessages = _engine
                .Schedule(() => _persistentUnackedMessageQueue.GetSyncedUnackedMessages().ToList())
                .Result;

            lock (_sync)
                _localQueue = new Queue<Tuple<int, ImmutableByteArray>>(unackedMessages);
            
            Notify();
        }

        public void Dispose()
        {
            _disposed = true;
            SafeTry(_socket.Dispose);
        }
    }
}
