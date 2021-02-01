using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace PBFT.Messages
{
    public class ViewChange : IProtocolMessages, SignedMessage
    {
        public byte[] SerializeToBuffer()
        {
            throw new System.NotImplementedException();
        }

        public static ViewChange DeSerializeToObject(byte[] buffer)
        {
            string jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<ViewChange>(jsonobj);
        }

        public void SignMessage(RSAParameters prikey, string haspro = "SHA256")
        {
            throw new System.NotImplementedException();
        }

        public IProtocolMessages CreateCopyTemplate()
        {
            throw new System.NotImplementedException();
        }
    }
}