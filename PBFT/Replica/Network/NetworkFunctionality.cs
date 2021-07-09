using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Replica.Network
{
    public static class NetworkFunctionality
    {
        //Receive is the function used to listen for incoming messages from a socket connection.
        //Receive is also responsible for deserialize the message properly based on its byte information.
        //Returns the message type and content.
        public static async Task<(List<int> mestype, List<IProtocolMessages> mes)> Receive(Socket conn)
        {
            try
            {
                var buffer = new byte[16000];
                var bytesread = await conn.ReceiveAsync(buffer, SocketFlags.None);
                List<IProtocolMessages> incommingMessages = new List<IProtocolMessages>();
                List<int> types = new List<int>();
                Console.WriteLine("Received a Message");
                if (bytesread == 0 || bytesread == -1) throw new SocketException();
                var bytemes = buffer
                    .ToList()
                    .Take(bytesread)
                    .ToArray();
                var jsonstringobj = Encoding.ASCII.GetString(bytemes);
                var mesobjects = jsonstringobj.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (mesobjects.Length > 1)
                {
                    Console.WriteLine("Duplicates detected!");
                    int idx = 0;
                    foreach (var mesjson in mesobjects)
                    {
                        byte[] bytesegment = bytemes.ToArray();
                        if (idx != 0)
                        {
                            bytesegment = bytesegment
                                .Skip(idx+1)
                                .ToArray();
                        }
                        var messegment = bytesegment.Take(mesjson.Length).ToArray();
                        var (type, mes) = Deserializer.ChooseDeserialize(messegment);
                        types.Add(type);
                        incommingMessages.Add(mes);
                        idx = mesjson.Length;
                    }
                }
                else
                {
                    var bytemesnodel = bytemes
                        .Take(bytemes.Length - 1)
                        .ToArray();
                    var (mestype, mes) = Deserializer.ChooseDeserialize(bytemesnodel);
                    types.Add(mestype);
                    incommingMessages.Add(mes);
                }
                return (types, incommingMessages);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error In Receive");
                Console.WriteLine(e.Message);
                throw;
            }
        }
        
        public static byte[] AddEndDelimiter(byte[] orghash)
        {
            byte[] copyobj = orghash.ToArray();
            byte[] resobj;
            resobj = copyobj.Concat(Encoding.ASCII.GetBytes("|")).ToArray();
            return resobj;
        }
        
        //Send sends out the given buffer to a given socket connection.
        public static void Send(Socket sock, byte[] buffer)
        {
            try
            {
                sock.Send(buffer, SocketFlags.None);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to send message!");
                Console.WriteLine(e.Message);
            }
        }
        
        //Connect attempts to connect the socket connection to the given IP address.
        //Whether or not the connect succeed or not is return as result.
        public static async Task<bool> Connect(Socket sock, IPEndPoint endpoint)
        {
            try
            {
                await sock.ConnectAsync(endpoint);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to connect to endpoint: " + endpoint.Address);
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}