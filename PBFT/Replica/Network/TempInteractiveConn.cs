using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PBFT.Replica.Network
{
    public class TempInteractiveConn
    {
        private string _ipAddress { get; }
        
        public IPEndPoint Address { get; }
        public Socket Socket { get; }

        public bool Active { get; set; }

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
            Active = false;
        }
        
        public async Task Connect()
        {
            while (!Socket.Connected)
                await NetworkFunctionality.Connect(Socket, Address);
            Active = true;
        }
        
        public void Dispose()
        {
            Socket.Dispose();
            Active= false;
        }
    }
}