using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
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
using Cleipnir.StorageEngine.SimpleFile;
using Newtonsoft.Json;
using PBFT.Certificates;
using PBFT.Network;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Replica
{
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
        public CheckpointCertificate StableCheckpointsCertificate;
        public SourceHandler Subjects { get; set; }
        private CDictionary<int, CList<ProtocolCertificate>> Log;
        public CDictionary<int, bool> ClientActive;
        public CDictionary<int, Reply> ReplyLog;
        public CDictionary<int, string> ServerContactList;
        public CDictionary<int, CArray<ViewChange>> ViewMessageRegister;
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
        private readonly object _sync = new object();
        private bool rebooted;

        public Server(int id, int curview, int totalreplicas, Engine sche, int checkpointinter, string ipaddress,
            SourceHandler sh, CDictionary<int, string> contactList) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            TotalReplicas = totalreplicas;
            CheckpointConstant = checkpointinter;
            CurPrimary = new ViewPrimary(TotalReplicas); //Leader of view 0 = server 0
            CurSeqRange = new Range(0, 2 * checkpointinter);
            Subjects = sh;
            Log = new CDictionary<int, CList<ProtocolCertificate>>();
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>();
            StableCheckpointsCertificate = null;
            CheckpointLog = new CDictionary<int, CheckpointCertificate>();

            _scheduler = sche;
            _servListener = new TempConnListener(ipaddress, HandleNewClientConnection);
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            //Console.WriteLine(BitConverter.ToString(Pubkey.Modulus));
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
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>();
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
            CurPrimary = lead;
            CheckpointConstant = seqRange.End.Value / 2;
            CurSeqRange = seqRange;
            Subjects = sh;
            Log = oldlog;
            ClientActive = new CDictionary<int, bool>();
            ReplyLog = new CDictionary<int, Reply>();
            ServerContactList = contactList;
            ViewMessageRegister = new CDictionary<int, CArray<ViewChange>>();
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
            int replicas, SourceHandler sh, CDictionary<int, CList<ProtocolCertificate>> oldlog,
            CDictionary<int, bool> clientActiveRegister, CDictionary<int, Reply> replog,
            CDictionary<int, string> contactList, CheckpointCertificate stablecheck, CDictionary<int, CheckpointCertificate> checkpoints)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            TotalReplicas = replicas;
            //_servConn = new TempConn(ipaddress);
            CurPrimary = lead;
            CheckpointConstant = checkpointint;
            CurSeqRange = seqRange;
            Subjects = sh;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
            ClientActive = clientActiveRegister;
            ReplyLog = replog;
            ServerContactList = contactList;
            ViewMessageRegister =
                new CDictionary<int, CArray<ViewChange>>(); //possibly change this to get stored view messages?
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

        public bool IsPrimary()
        {
            Start:
            Console.WriteLine("Isprimary");
            Console.WriteLine(CurView);
            Console.WriteLine(CurPrimary.ViewNr);
            if (CurView == CurPrimary.ViewNr)
            {
                if (ServID == CurPrimary.ServID) return true;
                return false;
            }

            CurPrimary.UpdateView(CurView);
            goto Start;
        }

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

        public void Start()
        {
            Console.WriteLine("Server starting");
            _ = _servListener.Listen();
            _ = ListenForStableCheckpoint();
        }

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

        public void AddEngine(Engine sche) => _scheduler = sche;

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

        //Handle incomming messages
        public async Task HandleIncommingMessages(TempInteractiveConn conn)
        {
            Console.WriteLine("New connection initialized!");
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
                                Session sesmes = (Session) mes;
                                DeviceType devtype = sesmes.Devtype;
                                Console.WriteLine(sesmes);
                                if(ServConnInfo.ContainsKey(sesmes.DevID)) Console.WriteLine(ServConnInfo[sesmes.DevID].Socket.Connected);
                                if (devtype == DeviceType.Client && (!ClientConnInfo.ContainsKey(sesmes.DevID) || !ClientConnInfo[sesmes.DevID].Socket.Connected))
                                {
                                    Console.WriteLine("New Session Message");
                                    await _scheduler.Schedule(() =>
                                    {
                                        MessageHandler.HandleSessionMessage(sesmes, conn, this);
                                        Session replysesmes = new Session(DeviceType.Server, Pubkey, ServID);
                                        Console.WriteLine("Returning message");
                                        SendMessage(replysesmes.SerializeToBuffer(), conn.Socket, MessageType.SessionMessage);
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
                                        SendMessage(replysesmes.SerializeToBuffer(), conn.Socket, MessageType.SessionMessage);
                                    });
                                }
                                break;
                            case MessageType.Request:
                                Request reqmes = (Request) mes;
                                if (ClientConnInfo.ContainsKey(reqmes.ClientID) &&
                                    ClientPubKeyRegister.ContainsKey(reqmes.ClientID))
                                {
                                    int idx = OperationInMemory(reqmes);
                                    if (idx == -1)
                                    {
                                        if (!ClientActive[reqmes.ClientID]) 
                                            await _scheduler.Schedule(() => 
                                            {
                                                Subjects.RequestSubject.Emit(reqmes);
                                            });
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
                                Console.WriteLine(mes);
                                if (ServConnInfo.ContainsKey(pesmes.ServID) &&
                                    ServPubKeyRegister.ContainsKey(pesmes.ServID))
                                {
                                    Console.WriteLine("Emitting"); //protocol, emit
                                    await _scheduler.Schedule(() =>
                                    {
                                        Subjects.ProtocolSubject.Emit(pesmes);
                                    });
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
                                if (ServConnInfo.ContainsKey(CurPrimary.ServID) &&
                                    ServPubKeyRegister.ContainsKey(CurPrimary.ServID))
                                {
                                    bool val = vc.Validate(ServPubKeyRegister[vc.ServID], vc.NextViewNr);
                                    if (val && ViewMessageRegister.ContainsKey(vc.NextViewNr)
                                    ) //will already have a view-change message for view n, therefore count = 2
                                    {
                                        ViewMessageRegister[vc.NextViewNr].Add(vc);
                                        ViewChangeCertificate vcc = new ViewChangeCertificate(
                                            new ViewPrimary(ServID, vc.NextViewNr, TotalReplicas), StableCheckpointsCertificate);
                                        foreach (var vctemp in ViewMessageRegister[vc.NextViewNr])
                                            vcc.AppendViewChange(vctemp, ServPubKeyRegister[vc.ServID]);
                                        await _scheduler.Schedule(() =>
                                        {
                                            Subjects.ShutdownSubject.Emit(vcc);
                                        });
                                    }
                                    else if (val && !ViewMessageRegister.ContainsKey(vc.NextViewNr)
                                    ) //does not already have any view-change messages for view n
                                    {
                                        ViewMessageRegister[vc.NextViewNr] = new CArray<ViewChange>();
                                        ViewMessageRegister[vc.NextViewNr].Add(vc);
                                    }
                                }
                                break;
                            case MessageType.NewView:
                                NewView nvmes = (NewView) mes;
                                if (ServConnInfo.ContainsKey(CurPrimary.ServID) &&
                                    ServPubKeyRegister.ContainsKey(CurPrimary.ServID))
                                {
                                    Console.WriteLine("Emitting");
                                    await _scheduler.Schedule(() =>
                                    {
                                        Subjects.NewViewSubject.Emit(nvmes);
                                    });
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
                                Console.WriteLine(check);
                                Console.WriteLine("CheckpointLenght: " + CheckpointLog.Count);
                                if (CheckpointLog.ContainsKey(check.StableSeqNr))
                                {
                                    if (StableCheckpointsCertificate == null || StableCheckpointsCertificate.LastSeqNr != check.StableSeqNr)
                                        //await _scheduler.Schedule(() =>
                                        //{
                                            CheckpointLog[check.StableSeqNr].AppendProof(
                                                check,
                                                ServPubKeyRegister[check.ServID],
                                                Quorum.CalculateFailureLimit(TotalReplicas)
                                            );
                                        //});
                                }
                                else if (!CheckpointLog.ContainsKey(check.StableSeqNr))
                                {
                                    //await _scheduler.Schedule(() =>
                                    //{
                                        CheckpointCertificate cert = new CheckpointCertificate(
                                            check.StableSeqNr,
                                            check.StateDigest, EmitCheckpoint
                                            );
                                        cert.AppendProof(
                                            check, 
                                            ServPubKeyRegister[check.ServID],
                                            Quorum.CalculateFailureLimit(TotalReplicas)
                                            );
                                        CheckpointLog[check.StableSeqNr] = cert;
                                        App.CreateCheckpoint(_scheduler, this);
                                    //});
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

        public void Multicast(byte[] sermessage, MessageType type)
        {
            Console.WriteLine("Multicasting: " + type);
            var mesidentbytes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            var fullbuffmes = NetworkFunctionality.AddEndDelimiter(mesidentbytes);
            foreach (var (sid, conn) in ServConnInfo)
            {
                if (sid != ServID) //shouldn't happen but just to be sure. Might be possible to use socket.SendAsync(mess, SocketFlag.Multicast)
                    conn.Socket.Send(fullbuffmes, SocketFlags.None);
            }
        }

        public void SendMessage(byte[] sermessage, Socket sock, MessageType type)
        {
            Console.WriteLine($"Sending: {type} message");
            var mesidentbytes = Serializer.AddTypeIdentifierToBytes(sermessage, type);
            var fullbuffmes = NetworkFunctionality.AddEndDelimiter(mesidentbytes);
            //Console.WriteLine("identifier?");
            //Console.WriteLine("Hello Mom");
            //Console.WriteLine("Sending message");
            sock.Send(fullbuffmes, SocketFlags.None);
        }

        public void EmitPhaseMessageLocally(PhaseMessage mes)
        {
            Console.WriteLine("Emitting Phase Locally!");
            //Console.WriteLine(mes);
            _scheduler.Schedule(() =>
            {
                Subjects.ProtocolSubject.Emit(mes);
            });
        }

        public void EmitCheckpoint(CheckpointCertificate cpc)
        {
            Console.WriteLine("Receieved stable checkpoint certificate, emitting");
            Console.WriteLine(CurSeqNr);

            _scheduler.Schedule(() =>
            {
                Subjects.CheckpointSubject.Emit(cpc);
            });
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
                        //var servConn = new TempConn(ip, false, null);
                        Console.WriteLine($"Initialize connection on {ip}");
                        var servConn = new TempInteractiveConn(ip);
                        await servConn.Connect();
                        Console.WriteLine("Connection established");
                        //ServConnInfo[k] = servConn;
                        Session sesmes = new Session(DeviceType.Server, Pubkey, ServID);
                        SendMessage(sesmes.SerializeToBuffer(), servConn.Socket, MessageType.SessionMessage);
                        _ = HandleIncommingMessages(servConn);
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
        public void InitializeLog(int seqNr) => Log[seqNr] = new CList<ProtocolCertificate>();
        
        public int NrOfLogEntries() => Log.Count;

        public void AddProtocolCertificate(int seqNr, ProtocolCertificate cert)
        {
            Console.WriteLine("Certificate saved!");
            Console.WriteLine("SeqNr:" + seqNr);
            Console.WriteLine($"Cert: {cert}");
            lock (_sync)
            {
                Log[seqNr].Add(cert);
            }
            SeeLog();
        }

        public void SeeLog()
        {
            foreach (var (seqNr,proofs) in Log)
            {
                Console.WriteLine("SeqNr: "+ seqNr);
                foreach (var proof in proofs) Console.WriteLine("Proof: " + proof);
            }
        }

        public int OperationInMemory(Request req)
        {
            Console.WriteLine("OperationInMemory");
            foreach (var (seqNr, rep) in ReplyLog)
            {
                //Console.WriteLine(rep);
                //Console.WriteLine(req);
                if (req.Message.Equals(rep.Result) && req.Timestamp.Equals(rep.Timestamp)) return seqNr;
            }
            return -1;
        }
        
        public CList<ProtocolCertificate> GetProtocolCertificate(int seqNr)
        {
            if (Log.ContainsKey(seqNr))
            {
                lock (_sync)
                {
                    return Log[seqNr];
                }    
            }
            else
            {
                return new CList<ProtocolCertificate>();
            }
            
        }

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
            return prepdict;
        }

        public async CTask ListenForStableCheckpoint()
        {
            Console.WriteLine("Listen for stable checkpoints");
            while (true)
            {
                var stablecheck = await Subjects.CheckpointSubject.Next();
                Console.WriteLine("Update Checkpoint State");
                StableCheckpointsCertificate = stablecheck;
                GarbageCollectLog(StableCheckpointsCertificate.LastSeqNr);
                GarbageCollectReplyLog(StableCheckpointsCertificate.LastSeqNr);
                GarbageCollectCheckpoints(StableCheckpointsCertificate.LastSeqNr);
                Console.WriteLine(StableCheckpointsCertificate);
            }
        }

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
                    //Console.WriteLine(digdict.Count);
                    string serializedlog = JsonConvert.SerializeObject(digdict);
                    //Console.WriteLine(serializedlog);
                    var bytelog = Encoding.ASCII.GetBytes(serializedlog);
                    return shaalgo.ComputeHash(bytelog);
                }
            }

            throw new ArgumentException();
        }

        public byte[] TestMakeStateDigest(int n) => MakeStateDigest(n);

        public void CreateCheckpoint(int limseqNr, CList<string> appstate)
        {
            if (StableCheckpointsCertificate == null || StableCheckpointsCertificate.LastSeqNr < limseqNr)
            {
                Console.WriteLine("Create checkpoint");
                //var statedig = MakeStateDigest(limseqNr);
                var statedig = Crypto.MakeStateDigest(appstate);
                CheckpointCertificate checkcert;
                if (CheckpointLog.ContainsKey(limseqNr)) checkcert = CheckpointLog[limseqNr];
                else checkcert = new CheckpointCertificate(limseqNr, statedig, EmitCheckpoint);
                var checkpointmes = new Checkpoint(ServID, limseqNr, statedig);
                checkpointmes.SignMessage(_prikey);
                Multicast(checkpointmes.SerializeToBuffer(), MessageType.Checkpoint); //wait for multicast to finish
                checkcert.AppendProof(checkpointmes, Pubkey, Quorum.CalculateFailureLimit(TotalReplicas));
            }
        }

        private void GarbageCollectLog(int seqNr)
        {
            foreach (var (entrySeqNr, _) in Log)
                if (entrySeqNr <= seqNr)
                    Log.Remove(entrySeqNr);
        }

        private void GarbageCollectReplyLog(int seqNr)
        {
            foreach (var (entrySeqNr, _) in ReplyLog)
                if (entrySeqNr <= seqNr)
                    Log.Remove(entrySeqNr);
        }

        private void GarbageCollectCheckpoints(int seqNr)
        {
            foreach (var (entrySeqNr, _) in CheckpointLog)
            {
                if (entrySeqNr <= seqNr)
                    CheckpointLog.Remove(entrySeqNr);
            }
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