
using System;
using PBFT.Helper;
using PBFT.Replica.Network;
using PBFT.Replica;

namespace PBFT.Messages
{
    public static class MessageHandler
    {
        public static void HandleSessionMessage(Session sesmes, TempInteractiveConn conn, Server serv)
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
                    serv.AddPubKeyClientRegister(id, sesmes.Publickey);
                    //serv.ClientPubKeyRegister[id] = sesmes.Publickey;
                }
                else
                {
                    if (!serv.ClientPubKeyRegister[id].Equals(sesmes.Publickey)) // Updated Client Connection
                    {
                        serv.ClientConnInfo[id].Dispose();
                        serv.ClientConnInfo[id] = conn;
                        serv.AddPubKeyClientRegister(id, sesmes.Publickey);
                        //serv.ClientPubKeyRegister[id] = sesmes.Publickey;
                    }
                }
            }
            else
            {
                if (!serv.ServConnInfo.ContainsKey(id)) //New Server Connections
                {
                    Console.WriteLine("Adding server");
                    //serv.ServConnInfo[id] = servconn;
                    serv.ServConnInfo[id] = conn;
                    //serv.ServPubKeyRegister[id] = sesmes.Publickey;
                    serv.AddPubKeyServerRegister(id, sesmes.Publickey);
                }
                else
                {
                    if (!serv.ServPubKeyRegister[id].Equals(sesmes.Publickey)) // Updated Server Connection
                    {
                        serv.ServConnInfo[id] = conn;
                        //serv.ServPubKeyRegister[id] = sesmes.Publickey;
                        serv.AddPubKeyServerRegister(id, sesmes.Publickey);
                    }
                }
            }
        }

        
    }
}