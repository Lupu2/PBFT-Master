using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using PBFT.Helper;
namespace PBFT.Messages
{
    
    public class SessionMessage : IProtocolMessages
    {
        public DeviceType Devtype {get; set;}
        public RSAParameters Publickey{get; set;}
        public int DevID {get; set;}

        public SessionMessage(DeviceType type, RSAParameters pubkey, int devid) 
        {
            Devtype = type;
            Publickey = pubkey;
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
            if (sesmes.Devtype != Devtype) return false;
            if (sesmes.DevID != DevID) return false;
            if (sesmes.Publickey.D != null && Publickey.Exponent != null && !sesmes.Publickey.D.SequenceEqual(Publickey.Exponent))
                return false;
            if (sesmes.Publickey.Modulus != null && Publickey.Modulus != null &&
                !sesmes.Publickey.Modulus.SequenceEqual(Publickey.Modulus))
                return false;
            return true;
        }
    }
}