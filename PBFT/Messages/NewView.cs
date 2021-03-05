using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Text;

namespace PBFT.Messages
{
    public class NewView : IProtocolMessages, SignedMessage
    {
        public byte[] SerializeToBuffer()
        {
            throw new System.NotImplementedException();
        }
        public static NewView DeSerializeToObject(byte[] buffer)
        {
            var jsonobj = Encoding.ASCII.GetString(buffer);
            return JsonConvert.DeserializeObject<NewView>(jsonobj);
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