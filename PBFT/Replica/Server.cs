using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.SimpleFile;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Replica.Network;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica.Protocol;

namespace PBFT.Replica
{
    //Server is our PBFT replica implementation.
    public class Server : IPersistable
    {
        //Persistent
        public int ServID { get; set; }
        public int CurView { get; set; }
        public ViewPrimary CurPrimary { get; set; }
        public int CurSeqNr { get; set; }
        public Range CurSeqRange { get; set; }
        public int CheckpointConstant { get; set; }
        public int TotalReplicas { get; set; }
        public bool ProtocolActive { get; set; }
        public CheckpointCertificate StableCheckpointsCertificate;
        public SourceHandler Subjects { get; set; }
        private CDictionary<int, CList<ProtocolCertificate>> Log;
        public CDictionary<int, bool> ClientActive;
        public CDictionary<int, Reply> ReplyLog;
        public CDictionary<int, string> ServerContactList;
        public CDictionary<int, ViewChangeCertificate> ViewMessageRegister;
        public CDictionary<int, CheckpointCertificate> CheckpointLog;

        //NON-Persitent
        private Engine _scheduler { get; set; }
        private TempConnListener _servListener { get; set; }
        private RSAParameters _prikey { get; set; }
        public RSAParameters Pubkey { get; set; }
        public Dictionary<int, TempInteractiveConn> ClientConnInfo;
        public Dictionary<int, TempInteractiveConn> ServConnInfo;
        public Dictionary<int, RSAParameters> ServPubKeyRegister;
        public Dictionary<int, RSAParameters> ClientPubKeyRegister;
        private bool rebooted;

        //Constructors
        public Server(int id, int curview, int totalreplicas, Engine sche, int checkpointinter, string ipaddress,
            SourceHandler sh, CDictionary<int, string> contactList) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            TotalReplicas = totalreplicas;
            ProtocolActive = false;
            CheckpointConstant = checkpointinter;
            CurPrimary = new ViewPrimary(TotalReplicas); //Leader of view 0 = server 0
            CurSeqRange = new Range(1, 2 * checkpointinter);
            Subjects = sh;
            Log = new CDictionary<int, CList<ProtocolCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, ViewChangeCertificate>();
            StableCheckpointsCertificate = null;
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

        public Server(int id, int curview, int seqnr, int totalreplicas, Engine sche, int checkpointinter,
            string ipaddress, SourceHandler sh, CDictionary<int, string> contactList)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = totalreplicas;
            ProtocolActive = false;
            CurPrimary = new ViewPrimary(TotalReplicas); //assume it is the leader, no it doesnt...
            CheckpointConstant = checkpointinter;
            if (seqnr - checkpointinter < 0) CurSeqRange = new Range(0, checkpointinter - seqnr);
            else CurSeqRange = new Range(checkpointinter, checkpointinter * 2);
            Subjects = sh;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<ProtocolCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, ViewChangeCertificate>();
            StableCheckpointsCertificate = null;
            CheckpointLog = new CDictionary<int, CheckpointCertificate>();

            _scheduler = sche;
            _servListener = new TempConnListener(ipaddress, HandleNewClientConnection);
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            ClientConnInfo = new Dictionary<int, TempInteractiveConn>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister[ServID] = Pubkey;
            ServConnInfo = new Dictionary<int, TempInteractiveConn>();
            rebooted = false;
        }

        public Server(int id, int curview, int seqnr, Range seqRange, Engine sche, string ipaddress, ViewPrimary lead,
            int replicas,
            SourceHandler sh, CDictionary<int, CList<ProtocolCertificate>> oldlog, CDictionary<int, string> contactList)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            ProtocolActive = false;
            CurPrimary = lead;
            CheckpointConstant = seqRange.End.Value / 2;
            CurSeqRange = seqRange;
            Subjects = sh;
            Log = oldlog;
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, ViewChangeCertificate>();
            StableCheckpointsCertificate = null;
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
        public Server(int id, int curview, int seqnr, int checkpointint, Range seqRange, Engine sche, ViewPrimary lead, 
            int replicas, bool active,  SourceHandler sh, CDictionary<int, CList<ProtocolCertificate>> oldlog,
            CDictionary<int, bool> clientActiveRegister, CDictionary<int, Reply> replog,
            CDictionary<int, string> contactList, CheckpointCertificate stablecheck, CDictionary<int, CheckpointCertificate> checkpoints)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            ProtocolActive = active;
            CurPrimary = lead;
            CheckpointConstant = checkpointint;
            CurSeqRange = seqRange;
            Subjects = sh;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
            ClientActive = clientActiveRegister;
            ReplyLog = replog;
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, ViewChangeCertificate>();
            StableCheckpointsCertificate = stablecheck;
            CheckpointLog = checkpoints;

            //Initialize non-persistent storage
            _servListener = new TempConnListener(ServerContactList[ServID], HandleNewClientConnection);
            ClientConnInfo = new Dictionary<int, TempInteractiveConn>();
            ServConnInfo = new Dictionary<int, TempInteractiveConn>();
            ClientPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister = new Dictionary<int, RSAParameters>();
            ServPubKeyRegister[ServID] = Pubkey;
            rebooted = true;
            _scheduler = sche;
        }

        //IsPrimary determines whether or not the replica is the primary replica.
        //Will also update the view information if the current view number is out of date.
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

        //SignMessage signs a given message object based on its message type.
        public void SignMessage(IProtocolMessages mes, MessageType type)
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
        }

        //Start starts the Server.
        public void Start()
        {
            Console.WriteLine("Server starting");
            _ = _servListener.Listen();
            _ = ListenForStableCheckpoint();
        }

        //Dispose disposes of all socket connections and resets the connection registries.
        public void Dispose()
        {
            _servListener.Dispose();
            foreach (var (id, cconn) in ClientConnInfo)
            {
                cconn.Dispose();
                ClientConnInfo.Remove(id);
            }

            foreach (var (id, sconn) in ServConnInfo)
            {
                sconn.Dispose();
                ServConnInfo.Remove(id);
            }
        }

        //AddEngine adds a Cleipnir execution engine to the Server object.
        public void AddEngine(Engine sche) => _scheduler = sche;

        //HandleNewClientConnection starts the process for handling established socket connections.
        public void HandleNewClientConnection(TempInteractiveConn conn)
        {
            if (_scheduler == null)
            {
                Console.WriteLine("Missing Engine");
                var teststorage = new SimpleFileStorageEngine("teststorage.txt", true);
                _scheduler = ExecutionEngineFactory.StartNew(teststorage);
                AddEngine(_scheduler);
            }

            _ = HandleIncommingMessages(conn);
        }

        //Handle incoming messages for a given socket connection.
        public async Task HandleIncommingMessages(TempInteractiveConn conn)
        {
            Console.WriteLine("New connection initialized!");
            while (true)
            {
                try
                {
                    var (mestypeList, mesList) = await NetworkFunctionality.Receive(conn.Socket);
                    int nrofInMess = mestypeList.Count;
                    for (int i = 0; i < nrofInMess; i++)
                    {
                        var mes = mesList[i];
                        var mesenum = Enums.ToEnumMessageType(mestypeList[i]);
                        Console.WriteLine("Type: " + mesenum);
                        switch (mesenum)
                        {
                            case MessageType.SessionMessage:
                                Session sesmes = (Session) mes;
                                DeviceType devtype = sesmes.Devtype;
                                Console.WriteLine(sesmes);
                                if (ServConnInfo.ContainsKey(sesmes.DevID))
                                    Console.WriteLine(ServConnInfo[sesmes.DevID].Socket.Connected);
                                if (devtype == DeviceType.Client && (!ClientConnInfo.ContainsKey(sesmes.DevID) ||
                                                                     !ClientConnInfo[sesmes.DevID].Socket.Connected))
                                {
                                    Console.WriteLine("New Session Message");
                                    await _scheduler.Schedule(() =>
                                    {
                                        MessageHandler.HandleSessionMessage(sesmes, conn, this);
                                        Session replysesmes = new Session(DeviceType.Server, Pubkey, ServID);
                                        Console.WriteLine("Returning message");
                                        SendMessage(replysesmes.SerializeToBuffer(), conn.Socket,
                                            MessageType.SessionMessage);
                                    });
                                }
                                else if (devtype == DeviceType.Server && sesmes.DevID != ServID &&
                                         (!ServConnInfo.ContainsKey(sesmes.DevID) ||
                                          !ServConnInfo[sesmes.DevID].Socket.Connected))
                                {
                                    Console.WriteLine("New Session Message");
                                    await _scheduler.Schedule(() =>
                                    {
                                        MessageHandler.HandleSessionMessage(sesmes, conn, this);
                                        Session replysesmes = new Session(DeviceType.Server, Pubkey, ServID);
                                        Console.WriteLine("Returning message");
                                        SendMessage(replysesmes.SerializeToBuffer(), conn.Socket,
                                            MessageType.SessionMessage);
                                    });
                                }

                                break;
                            case MessageType.Request:
                                Request reqmes = (Request) mes;
                                Console.WriteLine(reqmes);
                                if (ClientConnInfo.ContainsKey(reqmes.ClientID) &&
                                    ClientPubKeyRegister.ContainsKey(reqmes.ClientID))
                                {
                                    int idx = OperationInMemory(reqmes);
                                    if (idx == -1)
                                    {
                                        Console.WriteLine("Checking if client already has a working request");
                                        if (!ClientActive[reqmes.ClientID] && ProtocolActive)
                                            await _scheduler.Schedule(() =>
                                            {
                                                Console.WriteLine("Emitting request!");
                                                Subjects.RequestSubject.Emit(reqmes);
                                            });
                                        else
                                        {
                                            Console.WriteLine("Client request denied!");
                                        }
                                    }
                                    else SendMessage(ReplyLog[idx].SerializeToBuffer(), conn.Socket, MessageType.Reply);
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
                                Console.WriteLine(pesmes);
                                if (ServConnInfo.ContainsKey(pesmes.ServID) &&
                                    ServPubKeyRegister.ContainsKey(pesmes.ServID))
                                {
                                    MessageHandler.HandlePhaseMessage(
                                        pesmes, 
                                        CurView, 
                                        ProtocolActive, 
                                        _scheduler, 
                                        Subjects.ProtocolSubject, 
                                        Subjects.RedistSubject
                                    );
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
                                Console.WriteLine("New View-Change:");
                                Console.WriteLine(vc);
                                if (ServConnInfo.ContainsKey(CurPrimary.ServID) &&
                                    ServPubKeyRegister.ContainsKey(CurPrimary.ServID) ||
                                    CurPrimary.ServID == ServID)
                                    //MessageHandler.HandleViewChange(vc, this, _scheduler);
                                    MessageHandler.HandleViewChange2(vc, this, _scheduler);
                                break;
                            case MessageType.NewView:
                                NewView nvmes = (NewView) mes;
                                Console.WriteLine(nvmes);
                                if (ServConnInfo.ContainsKey(CurPrimary.ServID) &&
                                    ServPubKeyRegister.ContainsKey(CurPrimary.ServID))
                                {
                                    Console.WriteLine("Emitting");
                                    await _scheduler.Schedule(() => { Subjects.NewViewSubject.Emit(nvmes); });
                                }
                                else //Rules broken, terminate connection
                                {
                                    Console.WriteLine("Connection terminated, rules were broken");
                                    conn.Dispose();
                                    return;
                                }

                                break;
                            case MessageType.Checkpoint:
                                Checkpoint check = (Checkpoint) mes;
                                Console.WriteLine("Checkpoint: " + check);
                                await _scheduler.Schedule(() =>
                                {
                                    //MessageHandler.HandleCheckpoint(check, this);
                                    MessageHandler.HandleCheckpoint2(check, this);
                                });
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
                    Console.WriteLine("Error Message:" + e.Message);
                    return;
                }
            }
        }

        //Multicast first prepares a message to be send out to PBFT network and then multicasts it to each replica in its registry.
        public void Multicast(byte[] sermessage, MessageType type)
        {
            Console.WriteLine("Multicasting: " + type);
            var mesidentbytes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            var fullbuffmes = NetworkFunctionality.AddEndDelimiter(mesidentbytes);
            foreach (var (sid, conn) in ServConnInfo)
            {
                if (sid != ServID)
                    NetworkFunctionality.Send(conn.Socket, fullbuffmes);
            }
        }

        //SendMessage first prepares a message to be send out to PBFT network and then sends the message to desired connection.
        public void SendMessage(byte[] sermessage, Socket sock, MessageType type)
        {
            Console.WriteLine($"Sending: {type} message");
            var mesidentbytes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            var fullbuffmes = NetworkFunctionality.AddEndDelimiter(mesidentbytes);
            NetworkFunctionality.Send(sock, fullbuffmes);
        }
        
        //Emit Functions
        
        //EmitPhaseMessageLocally schedules an emit of a local phase message object to the normal protocol workflow.
        public void EmitPhaseMessageLocally(PhaseMessage mes)
        {
            Console.WriteLine("Emitting Phase Locally!");
            if (ProtocolActive)
            {
                _scheduler.Schedule(() =>
                {
                    Subjects.ProtocolSubject.Emit(mes);
                });    
            }
        }

        //EmitRedistPhaseMessageLocally schedules an emit of a local phase message object to the reprocess protocol workflow.
        public void EmitRedistPhaseMessageLocally(PhaseMessage mes)
        {
            Console.WriteLine("Emitting Redist Phase Locally");
            if (!ProtocolActive)
            {
                _scheduler.Schedule(() =>
                {
                    Subjects.RedistSubject.Emit(mes);
                });    
            }
        }

        //EmitViewChangeLocally schedules an emit of a local view-change message object to the view-change listeners.
        public void EmitViewChangeLocally(ViewChange viewmes)
        {
            Console.WriteLine("Emitting View Change Locally");
            _scheduler.Schedule(() =>
            {
                Subjects.ViewChangeSubject.Emit(viewmes);
            });
        }
        
        //EmitShutdown schedules an emit to the shutdown Source object which terminates any active protocol workflows.
        public void EmitShutdown()
        {
            Console.WriteLine("Received shutdown, emitting");
            if (ProtocolActive)
            {
                _scheduler.Schedule(() =>
                {
                    Subjects.ShutdownSubject.Emit(false);
                });    
            }
        }
        
        //EmitViewChange schedules an emit to the ViewChangeFin Source object to inform the view-change process that a valid view-change certificate is created.
        public void EmitViewChange()
        {
            Console.WriteLine("Received viewchange, emitting to start new view");
            _scheduler.Schedule(() =>
            {
                Subjects.ViewChangeFinSubject.Emit(true);
            });
        }
        
        //EmitCheckpoint schedules an emit to the CheckpointFin Source object to inform the checkpoint process that a stable checkpoint is ready.
        public void EmitCheckpoint(CheckpointCertificate cpc)
        {
            Console.WriteLine("Receieved stable checkpoint certificate, emitting");
            Console.WriteLine(CurSeqNr);
            _scheduler.Schedule(() =>
            {
                Subjects.CheckpointFinSubject.Emit(cpc);
            });
        }
        
        public void StartTimer(int length, CancellationToken cancel)
        {
            _= TimeoutOps.AbortableProtocolTimeoutOperationCTask(Subjects.ShutdownSubject, length, cancel);
        }
        
        //InitializeConnections initializes the socket connections for the replica.
        //When the starts it will only actively initialize socket connections for replicas with lower ids.
        //When persisted it will initiate a new socket connections for each replica in its registry.
        public async Task InitializeConnections() //Initialize Connections
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
                        Session sesmes = new Session(DeviceType.Server, Pubkey, ServID);
                        SendMessage(sesmes.SerializeToBuffer(), servConn.Socket, MessageType.SessionMessage);
                        _ = HandleIncommingMessages(servConn);
                    }
                }
            }
            else //Starting
            {
                foreach (var (k, ip) in ServerContactList)
                {
                    if (k != ServID && ServID > k)
                    {
                        Console.WriteLine($"Initialize connection on {ip}");
                        var servConn = new TempInteractiveConn(ip);
                        await servConn.Connect();
                        Console.WriteLine("Connection established");
                        Session sesmes = new Session(DeviceType.Server, Pubkey, ServID);
                        SendMessage(sesmes.SerializeToBuffer(), servConn.Socket, MessageType.SessionMessage);
                        _ = HandleIncommingMessages(servConn);
                    }
                }
            }
        }
        
        //ChangeClientStatus changes the client status for a given client.
        public void ChangeClientStatus(int cid)
        {
            //Assuming Client Already added during client initialization
            if (ClientActive[cid]) ClientActive[cid] = false;
            else ClientActive[cid] = true;
        }

        //ResetClientStatus resets the client active registry.
        public void ResetClientStatus()
        {
            foreach (var (cid, _) in ClientActive) ClientActive[cid] = false;
        }

        //AddPubkeyClientRegister adds a public key to the replicas client public key registry.
        public void AddPubKeyClientRegister(int id, RSAParameters key)
            => ClientPubKeyRegister[id] = key;
        
        //AddPubKeyServerRegister adds a public key to the replicas server public key registry.
        public void AddPubKeyServerRegister(int id, RSAParameters key)
            => ServPubKeyRegister[id] = key;
        
        //Log functions
        
        //InitializeLog initializes a persisted list for a protocol logger for a given sequence number. 
        public void InitializeLog(int seqNr) => Log[seqNr] = new CList<ProtocolCertificate>();
        
        //NrOfLogEntries returns the current protocol iterations in the replica's protocol log. 
        public int NrOfLogEntries() => Log.Count;

        //AddProtocolCertificate adds a protocol certificate for a protocol iteration to the replica's protocol log.
        public void AddProtocolCertificate(int seqNr, ProtocolCertificate cert)
        {
            Console.WriteLine("Saving Certificate: ");
            Console.WriteLine("SeqNr:" + seqNr);
            Console.WriteLine($"Cert: {cert}");
            Log[seqNr].Add(cert);
            SeeLog();
        }

        //SeeLog prints the current protocol log to the console window.
        public void SeeLog()
        {
            Console.WriteLine("Current Log: ");
            foreach (var (seqNr,proofs) in Log)
            {
                Console.WriteLine("SeqNr: "+ seqNr);
                foreach (var proof in proofs) Console.WriteLine("Proof: " + proof);
            }
        }
        
        //OperationInMemory checks whether or not the replica has a record for the given request.
        //Returns the sequence number if the request is found, but -1 if no replies in its log match the content of request.
        public int OperationInMemory(Request req)
        {
            Console.WriteLine("OperationInMemory");
            foreach (var (seqNr, rep) in ReplyLog)
            {
                if (req.Message.Equals(rep.Result) && req.Timestamp.Equals(rep.Timestamp)) return seqNr;
            }
            return -1;
        }
        
        //GetProtocolCertificate returns a list of protocol certificates for a given sequence number.
        public CList<ProtocolCertificate> GetProtocolCertificate(int seqNr)
        {
            if (Log.ContainsKey(seqNr)) return Log[seqNr];
            else return new CList<ProtocolCertificate>();
        } 
        
        //CollectPrepareCertificates collections protocol certificates for sequence number higher than the given stable sequence number.
        public CDictionary<int, ProtocolCertificate> CollectPrepareCertificates(int stableSeqNr)
        {
            CDictionary<int, ProtocolCertificate> prepdict = new CDictionary<int, ProtocolCertificate>();
            foreach (var (seqNr, certList) in Log)
            {
                if (seqNr > stableSeqNr)
                {
                    foreach (var cert in certList) //most likely always prep,commit order, but can't be completely sure
                        if (cert.CType == CertType.Prepared)
                            prepdict[seqNr] = cert;
                }
            }
            Console.WriteLine(prepdict.Count);
            return prepdict;
        }

        //Sequence number functions
        
        //UpdateRange updates the sequence number range.
        public void UpdateRange(int stableSeq)
            => CurSeqRange = new Range(stableSeq + 1, CurSeqRange.End.Value + (2 * CheckpointConstant));

        //UpdateSeqNr updates the replica's sequence number.
        public void UpdateSeqNr()
        {
            int newsqnr = 0;
            foreach (var (seqNr, certs) in Log)
            {
                if (seqNr > newsqnr && certs.Count == 2) newsqnr = seqNr;
            }
            CurSeqNr = newsqnr;
        }
        
        //MakeStateDigest creates a digest based on the n'th state of the protocol log.
        private byte[] MakeStateDigest(int n)
        {
            if (Log.Count > 0 && n > 0 && n <= Log.Count)
            {
                var digdict = new Dictionary<int, string>();
                foreach (var (seq, proofs) in Log)
                {
                    if (seq <= n)
                    {
                        var seqproof = JsonConvert.SerializeObject(Serializer.PrepareForSerialize(proofs));
                        digdict[seq] = seqproof;
                    }
                }

                using (var shaalgo = SHA256.Create()) //using: Dispose when finished with package 
                {
                    string serializedlog = JsonConvert.SerializeObject(digdict);
                    var bytelog = Encoding.ASCII.GetBytes(serializedlog);
                    return shaalgo.ComputeHash(bytelog);
                }
            }
            throw new ArgumentException();
        }

        //TestMakeStateDigest tests the MakeStateDigest function for a sequence number given number.
        public byte[] TestMakeStateDigest(int n) => MakeStateDigest(n);

        //CreateCheckpoint initializes and creates a checkpoint process.
        //outdated version.
        public void CreateCheckpoint(int limseqNr, CList<string> appstate)
        {
            if (StableCheckpointsCertificate == null || StableCheckpointsCertificate.LastSeqNr < limseqNr)
            {
                Console.WriteLine("Calling Create checkpoint");
                var statedig = Crypto.MakeStateDigest(appstate);
                CheckpointCertificate checkcert;
                Console.WriteLine("Scheduling checkpoint in created checkpoint");
                _scheduler.Schedule(() =>
                {
                    if (CheckpointLog.ContainsKey(limseqNr)) checkcert = CheckpointLog[limseqNr];
                    else
                    {
                        checkcert = new CheckpointCertificate(limseqNr, statedig, EmitCheckpoint);
                        CheckpointLog[limseqNr] = checkcert;
                    }
                    var checkpointmes = new Checkpoint(ServID, limseqNr, statedig);
                    checkpointmes.SignMessage(_prikey);
                    Multicast(checkpointmes.SerializeToBuffer(), MessageType.Checkpoint); //wait for multicast to finish
                    Console.WriteLine("CreateCheckpoint append");
                    Console.WriteLine(checkpointmes);
                    checkcert.AppendProof(checkpointmes, Pubkey, Quorum.CalculateFailureLimit(TotalReplicas));
                });
            }
        }

        //CreateCheckpoint initializes and creates a checkpoint process.
        //current version.
        public void CreateCheckpoint2(int limseqNr, CList<string> appstate)
        {
            if (StableCheckpointsCertificate == null || StableCheckpointsCertificate.LastSeqNr < limseqNr)
            {
                Console.WriteLine("Calling Create checkpoint");
                var statedig = Crypto.MakeStateDigest(appstate);
                Console.WriteLine("Scheduling checkpoint in created checkpoint");
                _scheduler.Schedule(() =>
                {
                    if (!CheckpointLog.ContainsKey(limseqNr))
                    {
                        CheckpointCertificate checkcert = new CheckpointCertificate(limseqNr, statedig, null);
                        CheckpointLog[limseqNr] = checkcert;
                        var checklistener = new CheckpointListener(
                        limseqNr,
                        Quorum.CalculateFailureLimit(TotalReplicas), 
                             statedig, 
                             Subjects.CheckpointSubject
                        );
                        _ = checklistener.Listen(checkcert, ServPubKeyRegister, EmitCheckpoint);
                    }
                    var checkmes = new Checkpoint(ServID, limseqNr, statedig);
                    checkmes.SignMessage(_prikey);
                    Multicast(checkmes.SerializeToBuffer(), MessageType.Checkpoint);
                    Console.WriteLine("CreateCheckpoint emitted");
                    Console.WriteLine(checkmes);
                    Subjects.CheckpointSubject.Emit(checkmes);
                });
            }
        }

        
        //Garbage Collection functions
        
        //ListenForStableCheckpoint listens for new stable checkpoint certificates. 
        //The Garbage collection is started once it receives a stable checkpoint certificate.
        public async CTask ListenForStableCheckpoint()
        {
            Console.WriteLine("Listen for stable checkpoints");
            while (true)
            {
                var stablecheck = await Subjects.CheckpointFinSubject.Next();
                Console.WriteLine("Update Checkpoint State");
                Console.WriteLine(stablecheck);
                StableCheckpointsCertificate = stablecheck;
                GarbageCollectLog(StableCheckpointsCertificate.LastSeqNr);
                GarbageCollectReplyLog(StableCheckpointsCertificate.LastSeqNr);
                GarbageCollectCheckpointLog(StableCheckpointsCertificate.LastSeqNr);
                UpdateRange(stablecheck.LastSeqNr);
            }
        }
        
        //GarbageCollectLog garbage collects the protocol logger up to the given stable sequence.
        private void GarbageCollectLog(int seqNr)
        {
            foreach (var (entrySeqNr, _) in Log)
                if (entrySeqNr <= seqNr)
                    Log.Remove(entrySeqNr);
        }

        //GarbageCollectReplyLog garbage collects the reply logger up to the given stable sequence.
        private void GarbageCollectReplyLog(int seqNr)
        {
            foreach (var (entrySeqNr, _) in ReplyLog)
                if (entrySeqNr <= seqNr)
                    Log.Remove(entrySeqNr);
        }

        //GarbageCollectViewChangeRegistry garbage collects the view-change registry up to the given stable sequence.
        public void GarbageViewChangeRegistry(int viewNr)
        {
            foreach (var (entryViewNr, _) in ViewMessageRegister)
                if (entryViewNr <= viewNr)
                    ViewMessageRegister.Remove(entryViewNr);
        }

        ////GarbageCollectCheckpointLog garbage collects the checkpoint logger up to the given stable sequence.
        private void GarbageCollectCheckpointLog(int seqNr)
        {
            foreach (var (entrySeqNr, _) in CheckpointLog)
                if (entrySeqNr <= seqNr)
                    CheckpointLog.Remove(entrySeqNr);
        }
        
        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(ServID), ServID);
            stateToSerialize.Set(nameof(CurView), CurView);
            stateToSerialize.Set(nameof(CurSeqNr), CurSeqNr);
            stateToSerialize.Set("CurSeqRangeLow", CurSeqRange.Start.Value);
            stateToSerialize.Set("CurSeqRangeHigh", CurSeqRange.End.Value);
            stateToSerialize.Set(nameof(CheckpointConstant), CheckpointConstant);
            stateToSerialize.Set(nameof(CurPrimary), CurPrimary);
            stateToSerialize.Set(nameof(TotalReplicas), TotalReplicas);
            stateToSerialize.Set(nameof(ProtocolActive), ProtocolActive);
            stateToSerialize.Set(nameof(Subjects), Subjects);
            stateToSerialize.Set(nameof(Log), Log);
            stateToSerialize.Set(nameof(ClientActive), ClientActive);
            stateToSerialize.Set(nameof(ReplyLog), ReplyLog);
            stateToSerialize.Set(nameof(ServerContactList), ServerContactList);
            stateToSerialize.Set(nameof(StableCheckpointsCertificate), StableCheckpointsCertificate);
            stateToSerialize.Set(nameof(CheckpointLog), CheckpointLog);
        }

        private static Server Deserialize(IReadOnlyDictionary<string, object> sd)
            => new Server(
                sd.Get<int>(nameof(ServID)),
                sd.Get<int>(nameof(CurView)),
                sd.Get<int>(nameof(CurSeqNr)),
                sd.Get<int>(nameof(CheckpointConstant)),
                new Range(sd.Get<int>("CurSeqRangeLow"), sd.Get<int>("CurSeqRangeHigh")),
                Engine.Current,
                sd.Get<ViewPrimary>(nameof(CurPrimary)),
                sd.Get<int>(nameof(TotalReplicas)),
                sd.Get<bool>(nameof(ProtocolActive)),
                sd.Get<SourceHandler>(nameof(Subjects)),
                sd.Get<CDictionary<int, CList<ProtocolCertificate>>>(nameof(Log)),
                sd.Get<CDictionary<int, bool>>(nameof(ClientActive)),
                sd.Get<CDictionary<int, Reply>>(nameof(ReplyLog)),
                sd.Get<CDictionary<int, string>>(nameof(ServerContactList)),
                sd.Get<CheckpointCertificate>(nameof(StableCheckpointsCertificate)),
                sd.Get<CDictionary<int, CheckpointCertificate>>(nameof(CheckpointLog))
        );
    }
}