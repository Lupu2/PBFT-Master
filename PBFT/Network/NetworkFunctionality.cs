using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Network
{
    public static class NetworkFunctionality
    {
        public static async Task<(List<int> mestype, List<IProtocolMessages> mes)> Receive(Socket conn)
        {
            try
            { 
                var buffer = new byte[4096];
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
                    Console.WriteLine("THIS IS A MESSAGE FROM LORD REX, WE ARE NOW IN BIG TROUBLE!");
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
                    //Console.WriteLine(BitConverter.ToString(bytemes));
                    var bytemesnodel = bytemes
                        .Take(bytemes.Length - 1)
                        .ToArray();
                    //Console.WriteLine(Encoding.ASCII.GetString(bytemesnodel));
                    var (mestype, mes) = Deserializer.ChooseDeserialize(bytemesnodel);
                    types.Add(mestype);
                    incommingMessages.Add(mes);
                }
                Console.WriteLine("finished reveived mes");
                return (types, incommingMessages);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error In Receive");
                Console.WriteLine(e);
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
        
        
    }
}