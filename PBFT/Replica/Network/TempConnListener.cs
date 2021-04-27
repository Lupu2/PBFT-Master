using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PBFT.Replica.Network
{
    public class TempConnListener
    {
        //https://www.youtube.com/watch?v=rrlRydqJbv0&t=244s
        //Template for Conn Object using Source for messages
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
        
        //Listener operations
        public async Task Listen()
        {
            Console.WriteLine("Calling binding");
            socket.Bind(endpoint);
            Console.WriteLine("Finished binding blade");
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