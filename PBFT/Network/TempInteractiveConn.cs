using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PBFT.Network
{
    public class TempInteractiveConn
    {
        private string _ipAddress { get; }
        
        public IPEndPoint Address { get; }
        public Socket Socket { get; }

        private bool _active = true;

        public TempInteractiveConn(Socket sock)
        {
            Address = (IPEndPoint) sock.RemoteEndPoint;
            _ipAddress = Address.ToString();
            Socket = sock;
        }

        public TempInteractiveConn(string ipAddress)
        {
            _ipAddress = ipAddress;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //IPv4 network
            Address = IPEndPoint.Parse(ipAddress);
            _active = false;
        }
        
        public async Task Connect() 
        {
            await Socket.ConnectAsync(Address);
            _active = true;
        }

        public void Dispose()
        {
            Socket.Dispose();
            _active = false;
        }
    }
}