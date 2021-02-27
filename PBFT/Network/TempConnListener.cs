using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.Rx;
using PBFT.Messages;

namespace PBFT.Network
{
    public class TempConnListener
    {
        //https://www.youtube.com/watch?v=rrlRydqJbv0&t=244s
        //Template for Conn Object using Source for messages
        private IPEndPoint endpoint;
        public Socket socket;
        private bool active;
        //private Source<IProtocolMessages> IncomingMessage;
        //private Source<IProtocolMessages> OutgoingMessage;
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

        // public async Task HandleConnection(Socket socketHandler)
        // {
        //     /*var clientSocket = await Task.Factory.FromAsync(
        //         new Func<AsyncCallback, object, IAsyncResult>(socket.BeginAccept),
        //         new Func<IAsyncResult, Socket>(socket.EndAccept),
        //         null).ConfigureAwait(false);*/
        //     var stream = new NetworkStream(socketHandler);
        //     var buffer = new byte[1024];
        //     while (true)
        //     {
        //         try
        //         {
        //             int bytesread = await stream.ReadAsync(buffer,0,buffer.Length);
        //             if (bytesread == 0 || bytesread == -1) break;
        //             //var obj = socketHandler.ReceiveAsync();
        //             var bytemes = buffer //want only the relevant part of the buffer.
        //                 .ToList()
        //                 .Take(bytesread)
        //                 .ToArray();
        //             var mes = Helper.Deserializer.ChooseDeserialize(bytemes);
        //             IncomingMessage.Emit(mes);
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine(e.Message);
        //         }
        //     }
        // }
        
        //Connecting/Sending operations
        
        public void Dispose()
        {
            socket.Dispose();
            active = false;
        }
    }
}