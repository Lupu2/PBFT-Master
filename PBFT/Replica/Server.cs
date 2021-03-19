using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.DataStructures;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using PBFT.Certificates;
using PBFT.Network;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Replica
{
    public class Server : IPersistable
    {
        //Persistent
        public int ServID {get; set;}
        public int CurView {get; set;}
        public ViewPrimary CurPrimary {get; set;}
        public int CurSeqNr {get; set;}
        public Range CurSeqRange { get; set;}
        
        public int CheckpointConstant { get; set; }
        public int TotalReplicas {get; set;}
        public CheckpointCertificate StableCheckpoints;
        public Source<Request> RequestBridge;
        public Source<PhaseMessage> ProtocolBridge;
        public Source<CheckpointCertificate> CheckpointBridge;
        private CDictionary<int, CList<ProtocolCertificate>> Log;
        public CDictionary<int, bool> ClientActive;
        public CDictionary<int, Reply> ReplyLog;
        public CDictionary<int, string> ServerContactList;
        public CDictionary<int, CArray<ViewChange>> ViewMessageRegister;
        

        public CDictionary<int, CheckpointCertificate> CheckpointLog;
        //NON-Persitent
        private Engine _scheduler {get; set;}
        private TempConnListener _servListener { get; set; }
        private RSAParameters _prikey{get; set;}
        public RSAParameters Pubkey{get; set;}
        public Dictionary<int, TempInteractiveConn> ClientConnInfo;
        public Dictionary<int, TempInteractiveConn> ServConnInfo;
        public Dictionary<int, RSAParameters> ServPubKeyRegister;
        public Dictionary<int, RSAParameters> ClientPubKeyRegister;
        private readonly object _sync = new object();
        private bool rebooted;
        
        public Server(int id, int curview, int totalreplicas, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int,string> contactList) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            TotalReplicas = totalreplicas;
            CheckpointConstant = checkpointinter;
            CurPrimary = new ViewPrimary(TotalReplicas); //Leader of view 0 = server 0
            CurSeqRange = new Range(0,2*checkpointinter);
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            Log = new CDictionary<int, CList<ProtocolCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>();
            
            _scheduler = sche;
            _servListener = new TempConnListener(ipaddress,HandleNewClientConnection);
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
            ClientConnInfo = new Dictionary<int, TempInteractiveConn>();
            ServConnInfo = new Dictionary<int, TempInteractiveConn>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister[ServID] = Pubkey;
            rebooted = false;
        }

        public Server(int id, int curview, int seqnr, int totalreplicas, Engine sche, int checkpointinter, string ipaddress, Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int,string> contactList)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = totalreplicas;
            CurPrimary = new ViewPrimary(TotalReplicas); //assume it is the leader, no it doesnt...
            CheckpointConstant = checkpointinter;
            if (seqnr - checkpointinter < 0) CurSeqRange = new Range(0, checkpointinter - seqnr);
            else CurSeqRange = new Range(checkpointinter, checkpointinter * 2);
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<ProtocolCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>();
            StableCheckpoints = null;
            CheckpointLog = new CDictionary<int, CheckpointCertificate>();
            
            _scheduler = sche;
            _servListener = new TempConnListener(ipaddress, HandleNewClientConnection);
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
            ClientConnInfo = new Dictionary<int, TempInteractiveConn>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister[ServID] = Pubkey;
            ServConnInfo = new Dictionary<int, TempInteractiveConn>();
            rebooted = false;
        }

        public Server(int id, int curview, int seqnr, Range seqRange, Engine sche, string ipaddress, ViewPrimary lead, 
            int replicas, Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int, CList<ProtocolCertificate>> oldlog, CDictionary<int,string> contactList)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            CurPrimary = lead;
            CheckpointConstant = seqRange.End.Value / 2;
            CurSeqRange = seqRange;
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            Log = oldlog;
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>();
            StableCheckpoints = null;
            CheckpointLog = new CDictionary<int, CheckpointCertificate>();
            
            _scheduler = sche;
            _servListener = new TempConnListener(ipaddress, HandleNewClientConnection);
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            ClientConnInfo = new Dictionary<int, TempInteractiveConn>();
            ServConnInfo = new Dictionary<int, TempInteractiveConn>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister[ServID] = Pubkey;
            rebooted = false;
        }

        [JsonConstructor]
        public Server(int id, int curview, int seqnr, int checkpointint, Range seqRange, ViewPrimary lead, int replicas, 
            Source<Request> reqbridge, Source<PhaseMessage> pesbridge, CDictionary<int, CList<ProtocolCertificate>> oldlog, 
            CDictionary<int, bool> clientActiveRegister, CDictionary<int, Reply> replog, CDictionary<int,string> contactList, 
            CheckpointCertificate stablecheck, CDictionary<int,CheckpointCertificate> checkpoints)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            //_servConn = new TempConn(ipaddress);
            CurPrimary = lead;
            CheckpointConstant = checkpointint;
            CurSeqRange = seqRange;
            RequestBridge = reqbridge;
            ProtocolBridge = pesbridge;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
            ClientActive = clientActiveRegister;
            ReplyLog = replog;
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>(); //possibly change this to get stored view messages?
            StableCheckpoints = stablecheck;
            CheckpointLog = checkpoints;
            
            //Initialize non-persistent storage
            _servListener = new TempConnListener(ServerContactList[ServID], HandleNewClientConnection);
            ClientConnInfo = new Dictionary<int, TempInteractiveConn>();
            ServConnInfo = new Dictionary<int, TempInteractiveConn>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister[ServID] = Pubkey;
            rebooted = true;
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

        public void Start()
        {
            Console.WriteLine("Server starting");
            _ = _servListener.Listen();
            _ = ListenForStableCheckpoint();
        } 
        
        public void HandleNewClientConnection(TempInteractiveConn conn)
        {
            //_scheduler.Schedule(() =>
            //{
                _ = HandleIncommingMessages(conn);
            //});
        }
        
        //Handle incomming messages
        public async Task HandleIncommingMessages(TempInteractiveConn conn)
        {
            while (true)
            {
                try
                {
                    var (mestypeList, mesList) = await NetworkFunctionality.Receive(conn.Socket);
                   //Console.WriteLine(ServPubKeyRegister.Count);
                   int nrofInMess = mestypeList.Count;
                   for (int i = 0; i < nrofInMess; i++)
                   {
                       var mes = mesList[i];
                       var mesenum = Enums.ToEnumMessageType(mestypeList[i]); 
                       Console.WriteLine("Type");
                       Console.WriteLine(mesenum);
                       switch (mesenum)
                       {
                           case MessageType.SessionMessage:
                               SessionMessage sesmes = (SessionMessage) mes;
                               DeviceType devtype = sesmes.Devtype;
                               if (devtype == DeviceType.Client && (!ClientConnInfo.ContainsKey(sesmes.DevID) ||
                                                                    !ClientConnInfo[sesmes.DevID].Socket.Connected))
                               {
                                   Console.WriteLine("New Session Message");
                                   MessageHandler.HandleSessionMessage(sesmes, conn, this);
                                   SessionMessage replysesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                                   await SendMessage(replysesmes.SerializeToBuffer(), conn.Socket,
                                       MessageType.SessionMessage);
                                   Console.WriteLine("Returning message");
                               }
                               else if (devtype == DeviceType.Server && sesmes.DevID != ServID && (!ServConnInfo.ContainsKey(sesmes.DevID) ||
                                                                         !ServConnInfo[sesmes.DevID].Socket.Connected))
                               {
                                   Console.WriteLine("New Session Message");
                                   MessageHandler.HandleSessionMessage(sesmes, conn, this);
                                   SessionMessage replysesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                                   await SendMessage(replysesmes.SerializeToBuffer(), conn.Socket,
                                       MessageType.SessionMessage);
                                   Console.WriteLine("Returning message");
                               }

                               break;
                           case MessageType.Request:
                               Console.WriteLine("New Request Message");
                               Request reqmes = (Request) mes;
                               if (ClientConnInfo.ContainsKey(reqmes.ClientID) &&
                                   ClientPubKeyRegister.ContainsKey(reqmes.ClientID))
                               {
                                   if (!ClientActive[reqmes.ClientID]) RequestBridge.Emit(reqmes);
                                   
                               }
                               else //Rules broken, terminate connection
                               {
                                   Console.WriteLine("Connection terminated, rules were broken");
                                   conn.Dispose();
                                   return;
                               }

                               break;
                           case MessageType.PhaseMessage:
                               Console.WriteLine("New PhaseMessage Message");
                               PhaseMessage pesmes = (PhaseMessage) mes;
                               Console.WriteLine(mes);
                               if (ServConnInfo.ContainsKey(pesmes.ServID) && ServPubKeyRegister.ContainsKey(pesmes.ServID))
                               {
                                   Console.WriteLine("Emitting");
                                   ProtocolBridge.Emit(pesmes);
                               }
                               else //Rules broken, terminate connection
                               {
                                   Console.WriteLine("Connection terminated, rules were broken");
                                   conn.Dispose();
                                   return;
                               }

                               break;
                           case MessageType.ViewChange:
                               ViewChange vc = (ViewChange) mes;
                               break;
                           case MessageType.NewView:
                               break;
                           case MessageType.Checkpoint:
                               Checkpoint check = (Checkpoint) mes;
                               if (CheckpointLog.ContainsKey(check.StableSeqNr) && StableCheckpoints.LastSeqNr != check.StableSeqNr)
                                   CheckpointLog[check.StableSeqNr].AppendProof(check, ServPubKeyRegister[check.ServID], Quorum.CalculateFailureLimit(TotalReplicas));
                               else if (!CheckpointLog.ContainsKey(check.StableSeqNr))
                               {
                                   CheckpointCertificate cert = new CheckpointCertificate(check.StableSeqNr, check.StateDigest);
                                   cert.AppendProof(check, ServPubKeyRegister[check.ServID], Quorum.CalculateFailureLimit(TotalReplicas));
                                   CheckpointLog[check.StableSeqNr] = cert;
                                   CreateCheckpoint(check.StableSeqNr);
                               }
                               break;
                           default:
                               Console.WriteLine("Unrecognizable Message");
                               break;
                       }
                   }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error In Handle Incomming Messages");
                    Console.WriteLine(e);
                    return;
                }
            }
        }
        
        public async Task Multicast(byte[] sermessage, MessageType type)
        {
            Console.WriteLine("Multicasting: " + type);
            var mesidentbytes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            var fullbuffmes = NetworkFunctionality.AddEndDelimiter(mesidentbytes);
            foreach(var(sid, conn) in ServConnInfo)
            {
                if (sid != ServID) //shouldn't happen but just to be sure. Might be possible to use socket.SendAsync(mess, SocketFlag.Multicast)
                    await conn.Socket.SendAsync(fullbuffmes, SocketFlags.None);
            }
        }

        public async Task SendMessage(byte[] sermessage, Socket sock, MessageType type)
        {
            Console.WriteLine($"Sending: {type} message");
            var mesidentbytes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            var fullbuffmes = NetworkFunctionality.AddEndDelimiter(mesidentbytes);
            //Console.WriteLine("identifier?");
            /*if (ServConnInfo.ContainsKey(id))
            {
                var conn = ServConnInfo[id];
                await conn.Socket.SendAsync(fullbuffmes, SocketFlags.None);
            }*/
            /*else if (ClientConnInfo.ContainsKey(id))
            {
                var conn = ClientConnInfo[id];
                await conn.Socket.SendAsync(fullbuffmes, SocketFlags.None);
            }*/
            //Console.WriteLine("Hello Mom");
            //Console.WriteLine("Sending message");
            await sock.SendAsync(fullbuffmes, SocketFlags.None);
            /*else //no info registered for this server
            {
                Console.WriteLine("no registered data");
            }*/
        }

        public void EmitPhaseMessageLocally(PhaseMessage mes)
        {
            Console.WriteLine("Emitting Phase Locally!");
            //Console.WriteLine(mes);
            ProtocolBridge.Emit(mes);
        }
        
        public async Task InitializeConnections() //Add Client To Client Dictionaries
        {
            if (rebooted) //Rebooting
            {
                foreach (var (k, ip) in ServerContactList)
                {
                    if (k != ServID)
                    {
                        Console.WriteLine($"Initialize connection on {ip}");
                        var servConn = new TempInteractiveConn(ip); 
                        await servConn.Connect();
                        Console.WriteLine("Connection established");
                        //ServConnInfo[k] = servConn;
                        SessionMessage sesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                        await SendMessage(sesmes.SerializeToBuffer(), servConn.Socket, MessageType.SessionMessage);
                        _= HandleIncommingMessages(servConn);
                    }
                }
            }
            else //Starting
            {
                foreach (var (k,ip) in ServerContactList)
                {
                    if (k != ServID && ServID>k)
                    {
                        //var servConn = new TempConn(ip, false, null);
                        Console.WriteLine($"Initialize connection on {ip}");
                        var servConn = new TempInteractiveConn(ip); 
                        await servConn.Connect();
                        Console.WriteLine("Connection established");
                        //ServConnInfo[k] = servConn;
                        SessionMessage sesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                        await SendMessage(sesmes.SerializeToBuffer(), servConn.Socket, MessageType.SessionMessage);
                        _= HandleIncommingMessages(servConn);
                    }
                }
            }
        }
        
        /*public async Task ReEstablishConnections()
        {
            foreach (var (k,conn) in ServConnInfo)
            {
                if (k != ServID)
                {
                    var servConn = new TempInteractiveConn(conn.Socket.RemoteEndPoint.ToString(), false, null);
                    await servConn.Connect();
                    //ServConnInfo[k] = servConn;
                    SessionMessage sesmes = new SessionMessage(DeviceType.Server, Pubkey, ServID);
                    await SendMessage(sesmes.SerializeToBuffer(), k, MessageType.SessionMessage);
                }
            }
        }*/
        
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

        public void AddPubKeyClientRegister(int id, RSAParameters key)
        {
            lock (_sync)
                ClientPubKeyRegister[id] = key;
        }

        public void AddPubKeyServerRegister(int id, RSAParameters key)
        {
            lock (_sync)
                ServPubKeyRegister[id] = key;
        }
        
        //Log functions
        public bool InitializeLog(int seqNr)
        {
            Console.WriteLine("Initialized Log");
            if (!Log.ContainsKey(seqNr)) Log[seqNr] = new CList<ProtocolCertificate>();
            else return false;
            return true;
        }
        
        //public void AddCertificate(int seqNr, ProtocolCertificate cert) => Log[seqNr].Add(cert);

        public void AddProtocolCertificate(int seqNr, ProtocolCertificate cert)
        {
            Console.WriteLine("Certificate saved!");
            lock (_sync)
            {
                Log[seqNr].Add(cert);   
            }
        }

        public async Task ListenForStableCheckpoint()
        {
            Console.WriteLine("Listen for stable checkpoints");
            while (true)
            {
                var stablecheck = await CheckpointBridge.Next();
                StableCheckpoints = stablecheck;
                GarbageCollect(StableCheckpoints.LastSeqNr);
            }
        }
        
        private byte[] MakeStateDigest(int N)
        { //TODO finish makestatedigest and have it make a digest of the log state up to N
            byte[] digest = new byte[]{1};
            return digest;
        }
        
        public void CreateCheckpoint(int limseqNr)
        {
            if (StableCheckpoints == null || StableCheckpoints.LastSeqNr < limseqNr)
            {
                var statedig = MakeStateDigest(limseqNr);
                CheckpointCertificate checkcert;
                if (CheckpointLog.ContainsKey(limseqNr)) checkcert = CheckpointLog[limseqNr];
                else checkcert = new CheckpointCertificate(limseqNr, statedig);
                var checkpointmes = new Checkpoint(ServID, limseqNr, statedig);
                checkpointmes.SignMessage(_prikey);
                Multicast(checkpointmes.SerializeToBuffer(), MessageType.Checkpoint).RunSynchronously();
                //Multicast(checkpointmes.SerializeToBuffer(), MessageType.Checkpoint).GetAwaiter().GetResult();
                checkcert.AppendProof(checkpointmes,Pubkey, Quorum.CalculateFailureLimit(TotalReplicas));    
            }
        }
        
        public void AddEngine(Engine sche) => _scheduler = sche;
        
        private void GarbageCollect(int seqNr)
        {
            foreach (var (entrySeqNr, _) in Log)
                if (entrySeqNr <= seqNr) 
                    Log.Remove(entrySeqNr);
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(CurView), CurView);
            stateToSerialize.Set(nameof(CurSeqNr), CurSeqNr);
            stateToSerialize.Set("CurSeqRangeLow", CurSeqRange.Start.Value);
            stateToSerialize.Set("CurSeqRangeHigh", CurSeqRange.End.Value);
            stateToSerialize.Set(nameof(CurPrimary), CurPrimary);
            stateToSerialize.Set(nameof(TotalReplicas), TotalReplicas);
            stateToSerialize.Set(nameof(RequestBridge), RequestBridge);
            stateToSerialize.Set(nameof(ProtocolBridge), ProtocolBridge);
            stateToSerialize.Set(nameof(Log), Log);
            stateToSerialize.Set(nameof(ClientActive), ClientActive);
            stateToSerialize.Set(nameof(ReplyLog), ReplyLog);
            stateToSerialize.Set(nameof(ServerContactList), ServerContactList);
            stateToSerialize.Set(nameof(StableCheckpoints), StableCheckpoints);
            stateToSerialize.Set(nameof(CheckpointLog), CheckpointLog);
        }
        
        private static Server Deserialize(IReadOnlyDictionary<string, object> sd)
        => new Server(
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(CurView)),
                sd.Get<int>(nameof(CurSeqNr)),
                sd.Get<int>(nameof(CheckpointConstant)),
                new Range(sd.Get<int>("CurSeqRangeLow"),sd.Get<int>("CurSeqRangeHigh")),
                sd.Get<ViewPrimary>(nameof(CurPrimary)),
                sd.Get<int>(nameof(TotalReplicas)),
                sd.Get<Source<Request>>(nameof(RequestBridge)),
                sd.Get<Source<PhaseMessage>>(nameof(ProtocolBridge)),
                sd.Get<CDictionary<int, CList<ProtocolCertificate>>>(nameof(Log)),
                sd.Get<CDictionary<int, bool>>(nameof(ClientActive)),
                sd.Get<CDictionary<int, Reply>>(nameof(ReplyLog)),
                sd.Get<CDictionary<int, string>>(nameof(ServerContactList)),
                sd.Get<CheckpointCertificate>(nameof(StableCheckpoints)),
                sd.Get<CDictionary<int, CheckpointCertificate>>(nameof(CheckpointLog))
        );
    }
}