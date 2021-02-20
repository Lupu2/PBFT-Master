using System;
using System.Net.Sockets;
using System.Threading;

namespace Playground.SimpleNetwork
{
    public class MessageSender
    {
        private readonly string _host;
        private readonly int _port;
        private Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        private readonly object _sync = new();

        public MessageSender(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Send(byte[] msg)
        {
            lock (_sync)
            {
                try
                {
                    while (!_socket.Connected)
                        _socket.Connect(_host, _port);

                    var lengthBytes = BitConverter.GetBytes(msg.Length);
                    _socket.Send(lengthBytes);
                    _socket.Send(msg);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(3000);
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
            }
        }
    }
}