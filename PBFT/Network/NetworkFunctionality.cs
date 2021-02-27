using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Network
{
    public static class NetworkFunctionality
    {
        public static async Task<(int mestype, IProtocolMessages mes)> Receive(Socket conn)
        {
            var buffer = new byte[1024];
            var bytesread = await conn.ReceiveAsync(buffer, SocketFlags.None);
            Console.WriteLine("Received a Message");
            if (bytesread == 0 || bytesread == -1) throw new SocketException();
            var bytemes = buffer
                .ToList()
                .Take(bytesread)
                .ToArray();
            var (mestype, mes) = Deserializer.ChooseDeserialize(bytemes);
            return (mestype, mes);
        }
    }
}