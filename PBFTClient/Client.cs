using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Network;
using PBFT.Replica;

namespace PBFT.Client
{
    public class Client
    {
        public int ClientID { get; }
        private RSAParameters _prikey{ get; } //Keep private key secret, can't leak info about: p,q & d
        public RSAParameters Pubkey { get; } //Contains only info for Exponent e & Modulus n
        
        public Dictionary<Request, ReplyCertificate> FinishedRequest;
        
        public Dictionary<int, ServerInfo> ServerInformation;

        public Source<Reply> ReplySource;

        public Engine _scheduler;
        
        public int FNumber { get; set; }

        public Client(int id)
        {
            ClientID = id;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();

            FinishedRequest = new Dictionary<Request, ReplyCertificate>();
            ServerInformation = new Dictionary<int, ServerInfo>();
            ReplySource = new Source<Reply>();
            _scheduler = ExecutionEngineFactory.StartNew(new InMemoryStorageEngine());
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

        public void SetFNumber()
        {
            int nrservers = ServerInformation.Count;
            switch (nrservers)
            {
                case 4:
                    FNumber = 1;
                    break;
                    
                case 7:
                    FNumber = 2;
                    break;
                case 10:
                    FNumber = 3;
                    break;
                default:
                    throw new IndexOutOfRangeException($"Server number {nrservers} not manageable!");
            }
        }

        public void ClientOperation()
        {
            while (true)
            {
                string op = CreateOperation();
                RunCommand(op).Wait();
                Console.WriteLine("RUN COMMAND FINISHED");
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
            bool val = await Task.WhenAny(ValidateRequest(req), TimeoutOps.TimeoutOperation(15000)).GetAwaiter().GetResult();
            //bool val = await ValidateRequest(req);
            Console.WriteLine("Finished await");
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

        private async Task SendSessionMessage(Session ses)
        {
            foreach (var (id, servinfo) in ServerInformation)
            {
                Console.WriteLine(id);
                byte[] sesbuff = NetworkFunctionality.AddEndDelimiter(
                    Serializer.AddTypeIdentifierToBytes(
                        ses.SerializeToBuffer(), MessageType.SessionMessage)
                    );
                await Send(servinfo.Socket, sesbuff);
            }
        }

        private async Task SendRequest(Request req)
        {
            foreach (var (id, servinfo) in ServerInformation)
            {
                byte[] reqbuff = NetworkFunctionality.AddEndDelimiter(
                    Serializer.AddTypeIdentifierToBytes(
                        req.SerializeToBuffer(), MessageType.Request)
                    );
                if (!servinfo.Active)
                {
                    var newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var conn = await NetworkFunctionality.Connect(newsock, IPEndPoint.Parse(servinfo.IPAddress));
                    if (conn)
                    {
                        servinfo.Socket = newsock;
                        servinfo.Active = true;
                        Session climes = new Session(DeviceType.Client, Pubkey, ClientID);
                        await SendSessionMessage(climes);
                        _ = ListenForResponse(newsock, id);
                    }
                }
                var res = await Send(servinfo.Socket, reqbuff); 
                if (!res) servinfo.Active = false;
            }
        }

        private async Task<bool> Send(Socket sock, byte[] buff)
        {
            try
            {
                await sock.SendAsync(buff, SocketFlags.None);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to send message");
                Console.WriteLine(e);
                return false;
            }
        }
        
        public async Task InitializeConnections()
        {
            foreach (var (id,info) in ServerInformation)
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //IPv4 network
                var endpoint = IPEndPoint.Parse(info.IPAddress);
                while (!sock.Connected) await NetworkFunctionality.Connect(sock, endpoint);
                info.Socket = sock;
                info.Active = true;
                _ = ListenForResponse(sock, id);
            }
            Session climes = new Session(DeviceType.Client, Pubkey, ClientID);
            await SendSessionMessage(climes);
            
        }
        
        public async Task ListenForResponse(Socket sock, int id)
        {
            while (true)
            {
                try
                {
                    var (mestypeList, mesList) = await NetworkFunctionality.Receive(sock);
                    int nrofmes = mestypeList.Count;
                    for (int i = 0; i < nrofmes; i++)
                    {
                        var mesenum = Enums.ToEnumMessageType(mestypeList[i]);
                        var mes = mesList[i];
                        switch (mesenum)
                        {
                            case MessageType.SessionMessage:
                                var sesmes = (Session) mes;
                                ServerInformation[sesmes.DevID].AddPubKeyInfo(sesmes.Publickey);
                                break;
                            case MessageType.Reply:
                                var replymes = (Reply) mes;
                                //ServerInformation[replymes.ServID].AddReply(replymes);
                                Console.WriteLine("Emitting reply");
                                await _scheduler.Schedule(() =>
                                {
                                    ReplySource.Emit(replymes);
                                });
                                break;
                            default:
                                Console.WriteLine("Unrecognized message!");
                                break;
                        }    
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR Listen Response");
                    Console.WriteLine(e);
                    ServerInformation[id].Active = false;
                    sock.Dispose();
                    return;
                }
            }
        }

        public async Task<bool> ValidateRequest(Request req)
        {
            try
            {
                var repCert = new ReplyCertificate(req, true); //most reply certificates are set to f+1 validation
                
                await ReplySource
                    .Where(rep => rep.Validate(ServerInformation[rep.ServID].GetPubkeyInfo(), req))
                    .Scan(repCert.ProofList, (prooflist, message) =>
                    {
                        prooflist.Add(message);
                        return prooflist;
                    })
                    .Where(_ => repCert.ValidateCertificate(FNumber))
                    .Next();
                Console.WriteLine("Received appropriate number of replies");
                Console.WriteLine(repCert.ProofList[0].Result);
                FinishedRequest[req] = repCert;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error ValidateRequest");
                Console.WriteLine(e);
                return false;
            }
        }
    }
}