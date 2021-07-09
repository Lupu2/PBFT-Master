using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PBFT.Replica.Network
{
    //TempConnListener is our implementation of a network listener.
    //Creates socket connections whenever a replica tries to connect to the replicas bound address.
    public class TempConnListener
    {
        //Initial inspiration: https://youtu.be/rrlRydqJbv0
        private IPEndPoint endpoint;
        public Socket socket;
        private bool active;
        private Action<TempInteractiveConn> newConnection;
        public TempConnListener(string ipAddress, Action<TempInteractiveConn> newConnCallback)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //IPv4 network
            endpoint = IPEndPoint.Parse(ipAddress);
            newConnection = newConnCallback;
            active = false;
        }

        public TempConnListener(IPEndPoint end, Socket sock)
        {
            socket = sock;
            endpoint = end;
            active = true;
        }
        
        //Listener operation
        public async Task Listen()
        {
            socket.Bind(endpoint);
            socket.Listen(128); //128 = default backlog value
            active = true;
            Console.WriteLine("Started Listening");
            while (active)
            {
                var cursocket = await socket.AcceptAsync();
                Console.WriteLine("Found socket");
                if (!active)
                    return;
                TempInteractiveConn clientconn = new TempInteractiveConn(cursocket);
                newConnection(clientconn);
            }
        }
        
        //Connecting/Sending operations
        public void Dispose()
        {
            socket.Dispose();
            active = false;
        }
    }
}