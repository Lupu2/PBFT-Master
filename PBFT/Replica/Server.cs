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

        public int NrOfReplicas {get; set;} //increment each time a server is added

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
        public CDictionary<int, TempConn> ServConnInfo;
        public CDictionary<int, TempClientConn> ClientConnInfo;

        public Dictionary<int, RSAParameters> ServPubKeyRegister;
        public Dictionary<int, RSAParameters> ClientPubKeyRegister;
        public Dictionary<int, bool> ClientActive;

        
        //TODO Update all constructors based on new objects parameters needed 
        public Server(int id, int curview, Engine sche, int checkpointinter, string ipaddress) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            NrOfReplicas = 1;
            _scheduler = sche;
            _servConn = new TempConn(ipaddress,true, HandleNewClientConnection);
            CurPrimary = new ViewPrimary(0,0); //Leader of view 0 = server 0
            CurSeqRange = new Range(0,checkpointinter);
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<QCertificate>>();
            
        }

        public Server(int id, int curview, int seqnr, Engine sche, int checkpointinter, string ipaddress)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = 1;
            _scheduler = sche;
            _servConn = new TempConn(ipaddress, true, HandleNewClientConnection);
            CurPrimary = new ViewPrimary(id,curview); //assume it is the leader
            if (seqnr - checkpointinter < 0) CurSeqRange = new Range(0, seqnr - checkpointinter);
            else CurSeqRange = new Range(checkpointinter, checkpointinter * 2);
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<QCertificate>>();
        }

        public Server(int id, int curview, int seqnr, Range seqRange, Engine sche, string ipaddress, ViewPrimary lead, 
            int replicas, CDictionary<int, CList<QCertificate>> oldlog)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = replicas;
            _scheduler = sche;
            _servConn = new TempConn(ipaddress, true, HandleNewClientConnection);
            CurPrimary = lead;
            CurSeqRange = seqRange;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
        }

        [JsonConstructor]
        public Server(int id, int curview, int seqnr, Range seqRange, ViewPrimary lead, int replicas, 
            CDictionary<int, CList<QCertificate>> oldlog)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = replicas;
            //_servConn = new TempConn(ipaddress);
            CurPrimary = lead;
            CurSeqRange = seqRange;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
        }

        public bool IsPrimary() => (ServID == CurPrimary.ServID && CurView == CurPrimary.ViewNr);

        public async void InitializeConnections(Dictionary<int,string> addresses) //Create Session messages and send them to other servers
        {
            
        }
        
        /*public async Conn Listen()
        {   //To be implemented
            
        }*/

        public void Start() => _ = _servConn.Listen();
        
        public void HandleNewClientConnection(TempClientConn conn)
        {
            _scheduler.Schedule(() =>
            {
                _ = HandleIncommingMessages(conn);
            });
        }
        
        //Handle incomming messages
        public async CTask HandleIncommingMessages(TempClientConn conn)
        {
            var clientSocket = await Task.Factory.FromAsync( //source: https://youtu.be/rrlRydqJbv0
                new Func<AsyncCallback, object, IAsyncResult>(conn._clientSock.BeginAccept),
                     new Func<IAsyncResult, Socket>(conn._clientSock.EndAccept),
                     null).ConfigureAwait(false);
            var stream = new NetworkStream(clientSocket);
            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesread = await stream.ReadAsync(buffer,0,buffer.Length);
                    if (bytesread == 0 || bytesread == -1) break;
                    var bytemes = buffer //want only the relevant part of the buffer.
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
                            break;
                        case MessageType.Request:
                            Request reqmes = (Request) mes;
                            if (ClientConnInfo.ContainsKey(reqmes.ClientID) && !ClientActive[reqmes.ClientID]) RequestBridge.Emit(reqmes);
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
        
        public async CTask Multicast(byte[] sermessage)
        {
            foreach(KeyValuePair<int,TempConn> conn in ServConnInfo)
            {
                //use Cleipnir network functionality together with conn
                
            }
        }

        public async CTask SendMessage(byte[] sermessage)
        {

        }
        
        public void InitializeClient(Dictionary<int,string> contactList) //Add Client To Client Dictionaries
        {
            foreach (var (k,ip) in contactList)
            {
                if (k == ServID || ServConnInfo.ContainsKey(k))
                {
                    var servConn = new TempConn(ip, false, null);
                    _ = servConn.Connect();
                    ServConnInfo[k] = servConn;
                }
            }
        }

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

        public void GarbageCollect(int seqNr)
        {
            foreach (var (entrySeqNr, entryLog) in Log)
                if (entrySeqNr < seqNr) 
                    Log.Remove(entrySeqNr);
        }
        
        
        //TODO Update serialization/deserialization parameters based on new parameters added
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(CurView), CurView);
            stateToSerialize.Set(nameof(CurSeqNr), CurSeqNr);
            stateToSerialize.Set("CurSeqRangeLow", CurSeqRange.Start.Value);
            stateToSerialize.Set("CurSeqRangeHigh", CurSeqRange.End.Value);
            stateToSerialize.Set(nameof(ViewPrimary), CurPrimary);
            stateToSerialize.Set(nameof(NrOfReplicas), NrOfReplicas);
            stateToSerialize.Set(nameof(Log), Log);
        }
        
        public void AddEngine(Engine sche) => _scheduler = sche;
        
        private static Server Deserialize(IReadOnlyDictionary<string, object> sd)
        => new Server(
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(CurView)),
                sd.Get<int>(nameof(CurSeqNr)),
                new Range(sd.Get<int>("CurSeqRangeLow"),sd.Get<int>("CurSeqRangeHigh")),
                sd.Get<ViewPrimary>(nameof(CurPrimary)),
                sd.Get<int>(nameof(NrOfReplicas)),
                sd.Get<CDictionary<int, CList<QCertificate>>>(nameof(Log))
            );
    }
}