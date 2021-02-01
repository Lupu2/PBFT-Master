using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace PBFT.Messages
{
    public enum DeviceType
    {
        Client,
        Server,
    }
    public class SessionMessage : IProtocolMessages
    {
        public DeviceType devtype {get; set;}
        public RSAParameters publickey{get; set;}
        public int DevID {get; set;}

        public SessionMessage(DeviceType type, RSAParameters pubkey, int devid) 
        {
            devtype = type;
            publickey = pubkey;
            DevID = devid;
        }

        public byte[] SerializeToBuffer()
        {
            string jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static SessionMessage DeSerializeToObject(byte[] buffer)
        {

            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<SessionMessage>(jsonobj);
        }
    }
}