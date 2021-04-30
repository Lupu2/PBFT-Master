using System.Net.Sockets;
using System.Security.Cryptography;

namespace PBFTClient
{
    public class ServerInfo
    {
        public int ServID {get;}
        public string IPAddress { get; }

        private RSAParameters _pubKey;
        //private CDictionary<int, Reply> _finishedRequests;
        public Socket Socket { get; set; }
        public bool Active { get; set; }
        
        public ServerInfo(int id, string ip)
        {
            ServID = id;
            IPAddress = ip;
            Active = false;
        }
        
        public void AddPubKeyInfo(RSAParameters pubkey) => _pubKey = pubkey;

        public RSAParameters GetPubkeyInfo() => _pubKey;

        //public void AddReply(Reply rep) => _finishedRequests[rep.SeqNr] = rep;
    }
    
    
}