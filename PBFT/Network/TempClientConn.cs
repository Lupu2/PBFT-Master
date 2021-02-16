using System;
using System.Net;
using System.Net.Sockets;

namespace PBFT.Network
{
    public class TempClientConn
    {
        private string _ipAddress { get; }
        
        public IPEndPoint _address { get; }
        public Socket _clientSock { get; }

        private bool _active = true;

        public TempClientConn(Socket sock)
        {
            _address = (IPEndPoint) sock.RemoteEndPoint;
            _ipAddress = _address.ToString();
            _clientSock = sock;
        }

        public void Dispose()
        {
            _clientSock.Dispose();
            _active = false;
        }
    }
}