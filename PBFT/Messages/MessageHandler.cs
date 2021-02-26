
using System;
using PBFT.Helper;
using PBFT.Network;
using PBFT.Replica;

namespace PBFT.Messages
{
    public static class MessageHandler
    {
        public static void HandleSessionMessage(SessionMessage sesmes, TempInteractiveConn conn, Server serv)
        {
            int id = sesmes.DevID;
            DeviceType devtype = sesmes.Devtype;
            Console.WriteLine("Handle session message");
            if (devtype == DeviceType.Client)
            {
                if (!serv.ClientConnInfo.ContainsKey(id)) //New Client Connections
                {
                    Console.WriteLine("Adding client");
                    serv.ClientActive[id] = false;
                    serv.ClientConnInfo[id] = conn;
                    serv.ClientPubKeyRegister[id] = sesmes.Publickey;
                }
                else
                {
                    if (!serv.ClientPubKeyRegister[id].Equals(sesmes.Publickey)) // Updated Client Connection
                    {
                        serv.ClientConnInfo[id].Dispose();
                        serv.ClientConnInfo[id] = conn;
                        serv.ClientPubKeyRegister[id] = sesmes.Publickey;
                    }
                }
            }
            else
            {
                if (!serv.ServConnInfo.ContainsKey(id)) //New Server Connections
                {
                    //serv.ServConnInfo[id] = servconn;
                    serv.ServConnInfo[id] = conn;
                    serv.ServPubKeyRegister[id] = sesmes.Publickey;
                }
                else
                {
                    if (!serv.ServPubKeyRegister[id].Equals(sesmes.Publickey)) // Updated Server Connection
                    {
                        serv.ServConnInfo[id].Dispose();
                        serv.ServConnInfo[id] = conn;
                        serv.ServPubKeyRegister[id] = sesmes.Publickey;
                    }
                }
            }
        }

        
    }
}