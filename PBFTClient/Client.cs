using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica.Network;

namespace PBFTClient
{
    //Client is our PBFT client implementation.
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
        
        //LoadServerInfo loads and initializes the client server information based on the information collected from the given JSON file.
        public void LoadServerInfo(string filename)
        {
            var serverdata = LoadJSONValues.LoadJSONFileContent(filename).Result;
            foreach (var servdata in serverdata)
            {
                var servInfo = new ServerInfo(servdata.Key, servdata.Value);
                ServerInformation[servdata.Key] = servInfo;
            }
        }

        //SetFNumber sets the number of faulty servers based on the number of entries in the clients server information.
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

        //ClientIOperation runs the client interacitve operation.
        public void ClientOperation()
        {
            while (true)
            {
                string op = CreateOperation();
                RunCommand(op).Wait();
                Console.WriteLine("RUN COMMAND FINISHED");
            }
        }
        
        //CreateOperation creates an operation based on input from the user.
        private string CreateOperation()
        {
            string op = ""; 
            bool done = false;
            while (!done)
            {
                Console.WriteLine("Write Operation:");
                op = Console.ReadLine();
                if (String.IsNullOrEmpty(op) || op.Contains("|"))
                {
                    Console.WriteLine("Not a valid operation!");
                    continue;
                }
                done = true;
            }
            return op;
        }
        
        //CreateOperations creates several operations based on input from the user.
        private List<string> CreateOperations()
        {
            List<string> operations = new List<string>();
            bool done = false;
            ConsoleKey resp = ConsoleKey.Clear;
            while (!done)
            {
                Console.WriteLine("Write operation:");
                string op = Console.ReadLine();
                if (String.IsNullOrEmpty(op) || op.Contains("|"))
                {
                    Console.WriteLine("Not a valid operation!");
                    continue;
                }
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

        //CreateRequest creates a client request based on the given message.
        private Request CreateRequest(string mes)
        {
            Request req = new Request(ClientID, mes, DateTime.Now.ToString()); //G, or empty
            req.SignMessage(_prikey);
            return req;
        }
        
        //RunCommand performs the operations related to sending a request and listening for replies.
        private async Task RunCommand(string op)
        {
            Request req = CreateRequest(op);
            Req:
            await SendRequest(req);
            bool val = await Task.WhenAny(ValidateRequest(req), TimeoutOps.TimeoutOperation(15000)).GetAwaiter().GetResult();
            Console.WriteLine("Finished waiting! Result: " + val);
            if (val) return;
            goto Req;
        }

        //Outdated
        private async Task RunCommands(List<string> ops)
        {
            foreach (string op in ops)
            {
                Request req = CreateRequest(op);
                await SendRequest(req);
            }
        }

        //SendSessionMessage first prepares the session messages to be send out to PBFT network and then multicast the message to its socket connections.
        private async Task SendSessionMessage(Session ses)
        {
            byte[] sesbuff = NetworkFunctionality.AddEndDelimiter(
                Serializer.AddTypeIdentifierToBytes(
                    ses.SerializeToBuffer(), MessageType.SessionMessage)
            );
            foreach (var (id, servinfo) in ServerInformation)
            {
                Console.WriteLine(id);
                
                await Send(servinfo.Socket, sesbuff);
            }
        }

        //SendRequest first prepares the request to be send out to PBFT network, then it multicast the request to each of its socket connections.
        //It will also attempt to reconnect to old socket connections.
        private async Task SendRequest(Request req)
        {
            byte[] reqbuff = NetworkFunctionality.AddEndDelimiter(
                Serializer.AddTypeIdentifierToBytes(
                    req.SerializeToBuffer(), MessageType.Request)
            );
            foreach (var (id, servinfo) in ServerInformation)
            {
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

        //Send simply sends the buffer out of the desired socket connection.
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
                Console.WriteLine(e.Message);
                return false;
            }
        }
        
        //InitializeConnections initializes the socket connections for the client to each replica in the PBFT network.
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
        
        //ListenForResponse listens for incoming messages to the client for a given socket connection.
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
                    Console.WriteLine("Error Listen Response");
                    Console.WriteLine("Error Message:" + e.Message);
                    ServerInformation[id].Active = false;
                    sock.Dispose();
                    return;
                }
            }
        }

        //ValidateRequest validates a request by waiting for the desired number replicas to send back their replies.
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