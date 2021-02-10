using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Messages;

namespace PBFT.Client
{
    public class ServerInfo
    {
        private int _servID;
        private string _ipAddress;
        private byte[] _pubKey;
        private CDictionary<int, Reply> _finishedRequests;
    }
}