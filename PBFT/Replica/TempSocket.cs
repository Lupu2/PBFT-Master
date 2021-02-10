using System;
using System.Net;
using System.Net.Sockets;

namespace PBFT.Replica
{
    public class TempSocket
    {
        private static Socket _servSocket;

        private string _ipAddress;

        public TempSocket(string ip)
        {
            _servSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ipAddress = ip;
        }
        
        
        public void Start()
        {
            Console.WriteLine("Server initialized...");
            //_servSocket.Bind(new IPEndPoint(IPAddress.Any));
            _servSocket.Listen(10); //can connect up to 5 at the time
            _servSocket.BeginAccept(new AsyncCallback(AccpetCallback), null);
        }

        private static void AccpetCallback(IAsyncResult AR)
        {
            Socket sock = _servSocket.EndAccept(AR);
            
        }
    }
}