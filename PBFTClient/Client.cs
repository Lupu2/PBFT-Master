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
using PBFT.Certificates;
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
        
        public Dictionary<Request, ReplyCertificate> FinishedRequest;
        
        public Dictionary<int, ServerInfo> ServerInformation;

        public Source<Reply> ReplySource;
        
        public int FNumber { get; set; }

        public Client(int id)
        {
            ClientID = id;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();

            FinishedRequest = new Dictionary<Request, ReplyCertificate>();
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
            {
                
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
            bool val = await Task.WhenAny(ValidateRequest(req), TimeoutOps.TimeoutOperation(5000)).Result;
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
                
                await servinfo.Socket.SendAsync(sesbuff, SocketFlags.None);
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
                                ReplySource.Emit(replymes);
                                break;
                            default:
                                Console.WriteLine("Unrecognized message!");
                                break;
                        }    
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR");
                    Console.WriteLine(e.Message);
                    ServerInformation[id].Active = false;
                    return;
                }
            }
        }

        public async Task<bool> ValidateRequest(Request req)
        {
            var repCert = new ReplyCertificate(req, true); //most reply certificates are set to f+1 validation
            
            //Set timeout for validateRequest and return false if it occurs
            //Console.WriteLine("Validating");
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
            if (repCert.ProofList[0].Result == "Failure") return false;
                FinishedRequest[req] = repCert;
            return true;
        }
    }
}