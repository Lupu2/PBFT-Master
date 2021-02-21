using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using PBFT.Helper;
namespace PBFT.Messages
{
    
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
            var jsonval = JsonConvert.SerializeObject(this);
            return Encoding.ASCII.GetBytes(jsonval);
        }

        public static SessionMessage DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<SessionMessage>(jsonobj);
        }

        public bool Compare(SessionMessage sesmes)
        {
            if (sesmes.devtype != devtype) return false;
            if (sesmes.DevID != DevID) return false;
            if (sesmes.publickey.D != null && publickey.Exponent != null && !sesmes.publickey.D.SequenceEqual(publickey.Exponent))
                return false;
            if (sesmes.publickey.Modulus != null && publickey.Modulus != null &&
                !sesmes.publickey.Modulus.SequenceEqual(publickey.Modulus))
                return false;
            return true;
        }
    }
}