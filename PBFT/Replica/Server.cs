using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using PBFT.Network;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Replica
{
    public class Server : IPersistable
    {
        public int ServID {get; set;}
        public int CurView {get; set;}
        public ViewPrimary CurPrimary {get; set;}
        public int CurSeqNr {get; set;}
        
        public Range CurSeqRange { get; set;}

        public int TotalReplicas {get; set;} 

        private Engine _scheduler {get; set;}
        
        private TempConn _servConn { get; set; }

        private CDictionary<int, CList<QCertificate>> Log; 
        
        private RSAParameters _prikey{get; set;} 

        public RSAParameters Pubkey{get; set;}

        public Source<Request> RequestBridge;

        public Source<PhaseMessage> ProtocolBridge;
        
        //public Source<IProtocolMessages> IncomingMessages;
        
        //public Source<IProtocolMessages> OutgoingMessages;
        //Connections information

        //Registers/Logs

        //int = Serv/ClientID
        public Dictionary<int, TempConn> ServConnInfo; //output connections
        public Dictionary<int, TempClientConn> ClientConnInfo;

        public Dictionary<int, RSAParameters> ServPubKeyRegister;
        public Dictionary<int, RSAParameters> ClientPubKeyRegister;
        public CDictionary<int, bool> ClientActive;
        public CDictionary<int, Reply> RequestLog;
        
        public Server(int id, int curview, int totalreplicas, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            TotalReplicas = totalreplicas;
            _scheduler = sche;
            _servConn = new TempConn(ipaddress,true, HandleNewClientConnection);
            CurPrimary = new ViewPrimary(TotalReplicas); //Leader of view 0 = server 0
            CurSeqRange = new Range(0,checkpointinter);
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<QCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
        }

        public Server(int id, int curview, int seqnr, int totalreplicas, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = totalreplicas;
            _scheduler = sche;
            _servConn = new TempConn(ipaddress, true, HandleNewClientConnection);
            CurPrimary = new ViewPrimary(TotalReplicas); //assume it is the leader
            if (seqnr - checkpointinter < 0) CurSeqRange = new Range(0, checkpointinter - seqnr);
            else CurSeqRange = new Range(checkpointinter, checkpointinter * 2);
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<QCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
        }

        public Server(int id, int curview, int seqnr, Range seqRange, Engine sche, string ipaddress, ViewPrimary lead, 
            int replicas, Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int, CList<QCertificate>> oldlog)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            _scheduler = sche;
            _servConn = new TempConn(ipaddress, true, HandleNewClientConnection);
            CurPrimary = lead;
            CurSeqRange = seqRange;
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
            ClientActive = new CDictionary<int, bool>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
        }

        [JsonConstructor]
        public Server(int id, int curview, int seqnr, Range seqRange, ViewPrimary lead, int replicas, 
            Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int, CList<QCertificate>> oldlog, CDictionary<int, bool> clientActiveRegister)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            //_servConn = new TempConn(ipaddress);
            CurPrimary = lead;
            CurSeqRange = seqRange;
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
            ClientActive = clientActiveRegister;
        }

        public bool IsPrimary()
        {
            Start:
            if (CurView == CurPrimary.ViewNr)
            {
                if (ServID == CurPrimary.ServID) return true;
                return false;
            }
            CurPrimary.UpdateView(CurView);
            goto Start;
        }
        
        /*public async Conn Listen()
        {   //To be implemented
            
        }*/

        public IProtocolMessages SignMessage(IProtocolMessages mes, MessageType type)
        {
            switch (type)
            {
                case MessageType.PhaseMessage:
                    PhaseMessage temppm = (PhaseMessage) mes;
                    temppm.SignMessage(_prikey);
                    mes = temppm;
                    break;
                case MessageType.Reply:
                    Reply tempry = (Reply) mes;
                    tempry.SignMessage(_prikey);
                    mes = tempry;
                    break;
                case MessageType.ViewChange:
                    ViewChange tempvc = (ViewChange) mes;
                    tempvc.SignMessage(_prikey);
                    mes = tempvc;
                    break;
                case MessageType.NewView:
                    NewView tempnv = (NewView) mes;
                    tempnv.SignMessage(_prikey);
                    mes = tempnv;
                    break;
                case MessageType.Checkpoint:
                    Checkpoint tempck = (Checkpoint) mes;
                    tempck.SignMessage(_prikey);
                    mes = tempck;
                    break;
                default:
                  throw new ArgumentOutOfRangeException();
            }

            return mes;
        }
        
        public void Start() => _ = _servConn.Listen();
        
        public void HandleNewClientConnection(TempClientConn conn)
        {
            //_scheduler.Schedule(() =>
            //{
                _ = HandleIncommingMessages(conn);
            //});
        }
        
        //Handle incomming messages
        public async Task HandleIncommingMessages(TempClientConn conn)
        {
            /*var clientSocket = await Task.Factory.FromAsync( //source: https://youtu.be/rrlRydqJbv0
                new Func<AsyncCallback, object, IAsyncResult>(conn._clientSock.BeginAccept),
                     new Func<IAsyncResult, Socket>(conn._clientSock.EndAccept),
                     null).ConfigureAwait(false);*/
            //var stream = new NetworkStream(clientSocket);
            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    //int bytesread = await stream.ReadAsync(buffer,0,buffer.Length);
                    //if (bytesread == 0 || bytesread == -1) break;
                    /*var bytemes = buffer //want only the relevant part of the buffer.
                        .ToList()
                        .Take(bytesread)
                        .ToArray();*/
                    var bytesread = await conn._clientSock.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesread == 0 || bytesread == -1) break;
                    var bytemes = buffer
                        .ToList()
                        .Take(bytesread)
                        .ToArray();
                    var (mestype, mes) = Deserializer.ChooseDeserialize(bytemes);
                    var mesenum = Enums.ToEnumMessageType(mestype);
                    switch (mesenum)
                    {
                        case MessageType.SessionMessage:
                            SessionMessage sesmes = (SessionMessage) mes;
                            MessageHandler.HandleSessionMessage(sesmes, conn, this);
                            SessionMessage replysesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                            await SendMessage(replysesmes.SerializeToBuffer(), sesmes.DevID, MessageType.SessionMessage);
                            break;
                        case MessageType.Request:
                            Request reqmes = (Request) mes;
                            if (ClientConnInfo.ContainsKey(reqmes.ClientID) && ClientPubKeyRegister.ContainsKey(reqmes.ClientID) && !ClientActive[reqmes.ClientID]) RequestBridge.Emit(reqmes);
                            break;
                        case MessageType.PhaseMessage:
                            PhaseMessage pesmes = (PhaseMessage) mes;
                            ProtocolBridge.Emit(pesmes);
                            break;
                        case MessageType.ViewChange:
                            break;
                        case MessageType.NewView:
                            break;
                        default:
                            Console.WriteLine("Unrecognizable Message");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        
        public async Task Multicast(byte[] sermessage, MessageType type)
        {
            var fullbuffmes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            foreach(var(sid, conn) in ServConnInfo)
            {
                if (sid != ServID) //shouldn't happen but just to be sure. Might be possible to use socket.SendAsync(mess, SocketFlag.Multicast)
                    await conn.socket.SendAsync(fullbuffmes, SocketFlags.None);
            }
        }

        public async Task SendMessage(byte[] sermessage, int id, MessageType type)
        {
            var fullbuffmes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            if (ServConnInfo.ContainsKey(id))
            {
                var conn = ServConnInfo[id];
                await conn.socket.SendAsync(fullbuffmes, SocketFlags.None);
            }
            else if (ClientConnInfo.ContainsKey(id))
            {
                var conn = ClientConnInfo[id];
                await conn._clientSock.SendAsync(fullbuffmes, SocketFlags.None);
            }
            else //no info registered for this server
            {
                Console.WriteLine("No data for the client being sent to");
            }
        }
        
        public async Task InitializeConnections(Dictionary<int,string> contactList) //Add Client To Client Dictionaries
        {
            foreach (var (k,ip) in contactList)
            {
                if (k != ServID && !ServConnInfo.ContainsKey(k) && ServID>k)
                {
                    var servConn = new TempConn(ip, false, null);
                    await servConn.Connect();
                    //ServConnInfo[k] = servConn;
                    SessionMessage sesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                    await SendMessage(sesmes.SerializeToBuffer(), k, MessageType.SessionMessage);
                    
                    //A - Leander system with lower id vs higher id 
                    //B - Input & Output Unique for server vs sockets unidirectional
                    //C - Complicated Algorithm
                }
            }
        }

        public async Task ReEstablishConnections()
        {
            foreach (var (k,conn) in ServConnInfo)
            {
                if (k != ServID)
                {
                    var servConn = new TempConn(conn.socket.RemoteEndPoint.ToString(), false, null);
                    await servConn.Connect();
                    //ServConnInfo[k] = servConn;
                    SessionMessage sesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                    await SendMessage(sesmes.SerializeToBuffer(), k, MessageType.SessionMessage);
                }
            }
        }
        
        /*public async CTask InitializeSession(Dictionary<int,string> addresses) //Create Session messages and send them to other servers
        {
            var sessionmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
            await Multicast(sessionmes.SerializeToBuffer());
        }*/

        public void ChangeClientStatus(int cid)
        {
            //Assuming Client Already added during client initialization
            if (ClientActive[cid]) ClientActive[cid] = false;
            else ClientActive[cid] = true;
        }
        
        //Log functions
        public bool InitializeLog(int seqNr)
        {
            if (!Log.ContainsKey(seqNr)) Log[seqNr] = new CList<QCertificate>();
            else return false;
            return true;
        }
        
        public CList<QCertificate> GetCertInfo(int seqNr) => Log[seqNr];
        
        public void AddCertificate(int seqNr, QCertificate cert) => Log[seqNr].Add(cert);

        public void AddEngine(Engine sche) => _scheduler = sche;
        
        public void GarbageCollect(int seqNr)
        {
            foreach (var (entrySeqNr, entryLog) in Log)
                if (entrySeqNr < seqNr) 
                    Log.Remove(entrySeqNr);
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(CurView), CurView);
            stateToSerialize.Set(nameof(CurSeqNr), CurSeqNr);
            stateToSerialize.Set("CurSeqRangeLow", CurSeqRange.Start.Value);
            stateToSerialize.Set("CurSeqRangeHigh", CurSeqRange.End.Value);
            stateToSerialize.Set(nameof(ViewPrimary), CurPrimary);
            stateToSerialize.Set(nameof(TotalReplicas), TotalReplicas);
            stateToSerialize.Set(nameof(RequestBridge), RequestBridge);
            stateToSerialize.Set(nameof(ProtocolBridge), ProtocolBridge);
            stateToSerialize.Set(nameof(Log), Log);
            stateToSerialize.Set(nameof(ClientActive), ClientActive);
        }
        
        private static Server Deserialize(IReadOnlyDictionary<string, object> sd)
        => new Server(
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(CurView)),
                sd.Get<int>(nameof(CurSeqNr)),
                new Range(sd.Get<int>("CurSeqRangeLow"),sd.Get<int>("CurSeqRangeHigh")),
                sd.Get<ViewPrimary>(nameof(CurPrimary)),
                sd.Get<int>(nameof(TotalReplicas)),
                sd.Get<Source<Request>>(nameof(RequestBridge)),
                sd.Get<Source<PhaseMessage>>(nameof(ProtocolBridge)),
                sd.Get<CDictionary<int, CList<QCertificate>>>(nameof(Log)),
                sd.Get<CDictionary<int, bool>>(nameof(ClientActive))
            );
    }
}