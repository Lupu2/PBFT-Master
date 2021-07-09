using System.Net.Sockets;
using System.Security.Cryptography;

namespace PBFTClient
{
    //ServerInfo is an object that the client uses to store information about a replica.
    public class ServerInfo
    {
        public int ServID {get;}
        public string IPAddress { get; }
        private RSAParameters _pubKey;
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
    }
    
    
}