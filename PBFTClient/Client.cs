using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Network;

namespace PBFT.Client
{
    public class Client
    {
        
        public int ClientID { get; }
        private RSAParameters _prikey{ get; } //Keep private key secret, can't leak info about: p,q & d
        public RSAParameters Pubkey { get; } //Contains only info for Exponent e & Modulus n

        public Request CurReq { get; set; }
        
        public Dictionary<int, string> FinishedRequest;
        
        public Dictionary<int, ServerInfo> ServerInformation;

        public Source<Reply> ReplySource;
        
        
        public Client(int id)
        {
            ClientID = id;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();

            FinishedRequest = new Dictionary<int, string>();
            ServerInformation = new Dictionary<int, ServerInfo>();
            ReplySource = new Source<Reply>();
        }
        
        public void LoadServerInfo(string filename)
        {
            var serverdata = LoadJSONValues.LoadJSONFileContent(filename).Result;
            foreach (var servdata in serverdata)
            {
                var servInfo = new ServerInfo(servdata.Key, servdata.Value);
                ServerInformation[servdata.Key] = servInfo;
            }
        }

        public void ClientOperation()
        {
            while (true)
            {
                string op = CreateOperation();
                _ = RunCommand(op);
            }
        }
        
        private string CreateOperation()
        {
            string op = ""; 
            bool done = false;
            while (!done)
            {
                Console.WriteLine("Write Operation:");
                op = Console.ReadLine();
                if (String.IsNullOrEmpty(op)) continue;
                done = true;
            }

            return op;
        }
        
        private List<string> CreateOperations()
        {
            List<string> operations = new List<string>();
            bool done = false;
            ConsoleKey resp = ConsoleKey.Clear;
            while (!done)
            {
                Console.WriteLine("Write operation:");
                string op = Console.ReadLine();
                if (String.IsNullOrEmpty(op)) continue;
                operations.Add(op);
                Console.WriteLine("Done creating operations?[y/n]"); //https://stackoverflow.com/questions/37359161/how-would-i-make-a-yes-no-prompt-in-console-using-c
                bool conf = false;
                while (!conf)
                {
                    confirmation: 
                    resp = Console.ReadKey(false).Key;
                    if (resp != ConsoleKey.Y && resp != ConsoleKey.N) goto confirmation;
                    conf = true;
                }

                if (resp == ConsoleKey.Y) done = true;
            }
            return operations;
        }

        private Request CreateRequest(string mes)
        {
            Request req = new Request(ClientID, mes, DateTime.Now.ToString()); //G, or empty
            req.SignMessage(_prikey);
            return req;
        }
        
        private async Task RunCommand(string op)
        {
            Request req = CreateRequest(op);
            Req:
            await SendRequest(req);
            bool val = await ValidateRequest(req);
            if (val) return;
            goto Req;
        }

        private async Task RunCommands(List<string> ops)
        {
            foreach (string op in ops)
            {
                Request req = CreateRequest(op);
                await SendRequest(req);
            }
        }

        private async Task SendRequest(Request req)
        {
            foreach (var (id,servinfo) in ServerInformation)
            {
                byte[] reqbuff = Serializer.AddTypeIdentifierToBytes(req.SerializeToBuffer(), MessageType.Request);
                await servinfo.Socket.SendAsync(reqbuff, SocketFlags.None);
            }
        }

        public async Task InitializeConnections()
        {
            foreach (var (id,info) in ServerInformation)
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //IPv4 network
                var endpoint = IPEndPoint.Parse(info.IPAddress);
                while (!sock.Connected) await sock.ConnectAsync(endpoint);
                info.Socket = sock;
                info.active = true;
                _ = ListenForResponse(sock, id);
            }
        }
        
        public async Task ListenForResponse(Socket sock, int id)
        {
            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    var (mestype, mes) = await NetworkFunctionality.Receive(sock);
                    var mesenum = Enums.ToEnumMessageType(mestype);
                    switch (mesenum)
                    {
                        case MessageType.SessionMessage:
                            var sesmes = (SessionMessage) mes;
                            ServerInformation[sesmes.DevID].AddPubKeyInfo(sesmes.Publickey);
                            break;
                        case MessageType.Reply:
                            var replymes = (Reply) mes;
                            //ServerInformation[replymes.ServID].AddReply(replymes);
                            ReplySource.Emit(replymes);
                            break;
                        default:
                            Console.WriteLine("Unrecognized message!");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    ServerInformation[id].active = false;
                    return;
                }
            }
        }

        public async Task<bool> ValidateRequest(Request req)
        {
            var replies = await ReplySource
                                .Where(rep => rep.Timestamp == req.Timestamp)
                                .Next();
            return true;
        }
    }
}