using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Messages;

namespace PBFT.Client
{
    public class ServerInfo
    {
        public int ServID {get;}
        public string IPAddress { get; }
        private RSAParameters _pubKey;
        private CDictionary<int, Reply> _finishedRequests;
        public Socket Socket { get; set; }
        public bool active { get; set; }
        
        public ServerInfo(int id, string ip)
        {
            ServID = id;
            IPAddress = ip;
            active = false;
        }
        
        public void AddPubKeyInfo(RSAParameters pubkey) => _pubKey = pubkey;
        public void AddReply(Reply rep) => _finishedRequests[rep.SeqNr] = rep;
    }
    
    
}