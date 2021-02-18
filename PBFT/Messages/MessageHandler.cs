
using PBFT.Helper;
using PBFT.Network;
using PBFT.Replica;

namespace PBFT.Messages
{
    public static class MessageHandler
    {
        public static void HandleSessionMessage(SessionMessage sesmes, TempClientConn conn, Server serv)
        {
            int id = sesmes.DevID;
            DeviceType devtype = sesmes.devtype;
                        
            if (devtype == DeviceType.Client)
            {
                if (!serv.ClientConnInfo.ContainsKey(id)) //New Client Connections
                {
                    serv.ClientConnInfo[id] = conn;
                    serv.ClientPubKeyRegister[id] = sesmes.publickey;
                }
                else
                {
                    if (!serv.ClientPubKeyRegister[id].Equals(sesmes.publickey)) // Updated Client Connection
                    {
                        serv.ClientConnInfo[id].Dispose();
                        serv.ClientConnInfo[id] = conn;
                        serv.ClientPubKeyRegister[id] = sesmes.publickey;
                    }
                }
            }
            else
            {
                TempConn servconn = new TempConn(conn._address, conn._clientSock); //casting it to a server conn
                if (!serv.ServConnInfo.ContainsKey(id)) //New Server Connections
                {
                    serv.ServConnInfo[id] = servconn;
                    serv.ServPubKeyRegister[id] = sesmes.publickey;
                }
                else
                {
                    if (!serv.ServPubKeyRegister[id].Equals(sesmes.publickey)) // Updated Server Connection
                    {
                        serv.ServConnInfo[id].Dispose();
                        serv.ServConnInfo[id] = servconn;
                        serv.ServPubKeyRegister[id] = sesmes.publickey;
                    }
                }
            }
        }

        
    }
}