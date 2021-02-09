using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using PBFT.Network;
using PBFT.Helper;

namespace PBFT.Server
{
    public class Server
    {
        public int ServID {get; set;}
        public int CurView {get; set;}
        public ViewPrimary CurPrimary {get; set;}
        public int CurSeqNr {get; set;}

        public int NrOfReplicas {get; set;} //increment each time a server is added

        private Engine _scheduler {get; set;}

        private object _sync = new object();

        private CDictionary<int, CList<QCertificate>> Log = new(); 
        
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

        public Server(int id, int curview, Engine sche) //Initial constructor
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = 0;
            NrOfReplicas = 1;
            _scheduler = sche;
            CurPrimary = new ViewPrimary(0,0); //Leader of view 0 = server 0
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
        }

        public Server(int id, int curview, int seqnr, Engine sche)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = 1;
            _scheduler = sche;
            CurPrimary = new ViewPrimary(id,curview); //assume it is the leader
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
        }

        public Server(int id, int curview, int seqnr, Engine sche, ViewPrimary lead, int replicas)
        {
            ServID = id;
            CurView = curview;
            CurSeqNr = seqnr;
            NrOfReplicas = replicas;
            _scheduler = sche;
            CurPrimary = lead;
            (_prikey,Pubkey) = Crypto.InitializeKeyPairs();
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


    }
}