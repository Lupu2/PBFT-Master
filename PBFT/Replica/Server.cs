using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using PBFT.Network;
using PBFT.Helper;

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

        private CDictionary<int, CList<QCertificate>> Log; 
        
        private RSAParameters _prikey{get; set;} 

        public RSAParameters Pubkey{get; set;}
        //Connections information

        //Registers/Logs

        //int = Serv/ClientID
        public CDictionary<int, Conn> ServConnInfo;
        public CDictionary<int, Conn> ClientConnInfo;

        public Dictionary<int, RSAParameters> ServPubKeyRegister;
        public Dictionary<int, RSAParameters> ClientPubKeyRegister;
        public Dictionary<int, bool> ClientActive;

        public Server(int id, int curview, Engine sche, int checkpointinter) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            NrOfReplicas = 1;
            _scheduler = sche;
            CurPrimary = new ViewPrimary(0,0); //Leader of view 0 = server 0
            CurSeqRange = new Range(0,checkpointinter);
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<QCertificate>>();
        }

        public Server(int id, int curview, int seqnr, Engine sche, int checkpointinter)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = 1;
            _scheduler = sche;
            CurPrimary = new ViewPrimary(id,curview); //assume it is the leader
            if (seqnr - checkpointinter < 0) CurSeqRange = new Range(0, seqnr - checkpointinter);
            else CurSeqRange = new Range(checkpointinter, checkpointinter * 2);
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = new CDictionary<int, CList<QCertificate>>();
        }

        public Server(int id, int curview, int seqnr, Range seqRange, Engine sche, ViewPrimary lead, int replicas, CDictionary<int, CList<QCertificate>> oldlog)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = replicas;
            _scheduler = sche;
            CurPrimary = lead;
            CurSeqRange = seqRange;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
        }
        
        [JsonConstructor]
        public Server(int id, int curview, int seqnr, Range seqRange, ViewPrimary lead, int replicas, CDictionary<int, CList<QCertificate>> oldlog)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = replicas;
            CurPrimary = lead;
            CurSeqRange = seqRange;
            (_prikey, Pubkey) = Crypto.InitializeKeyPairs();
            Log = oldlog;
        }
        
        /* public bool IsPrimary()
        {
            if (ServID == CurPrimary.ServID && CurView == CurPrimary.ViewNr) return true;
            return false;
        } */

        public bool IsPrimary() => (ServID == CurPrimary.ServID && CurView == CurPrimary.ViewNr);

        public async void InitializeConnections(Dictionary<int,string> addresses) //Create Session messages and send them to other servers
        {
            
        }

        /*public async Conn Listen()
        {   //To be implemented
            
        }*/

        public async CTask Multicast(byte[] sermessage)
        {
            foreach(KeyValuePair<int,Conn> conn in ServConnInfo)
            {
                //use Cleipnir network functionality together with conn
                
            }
        }

        public async CTask SendMessage(byte[] sermessage)
        {

        }



        public void InitializeClient() //Add Client To Client Dictionaries
        {
    
        }

        public void ChangeClientStatus(int cid)
        {
            //Assuming Client Already added during client initialization
            if (ClientActive[cid]) ClientActive[cid] = false;
            else    ClientActive[cid] = true;
        }

        
        //Log functions
        public void InitializeLog(int seqNr) => Log[seqNr] = new CList<QCertificate>();

        public CList<QCertificate> GetCertInfo(int seqNr) => Log[seqNr];
        
        public void AddCertificate(int seqNr, QCertificate cert) => Log[seqNr].Add(cert);

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