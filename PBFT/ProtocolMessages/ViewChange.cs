using System.Security.Cryptography;

namespace PBFT.ProtocolMessages
{
    public class ViewChange : IProtocolMessages<ViewChange>
    {
        public byte[] SerializeToBuffer()
        {
            throw new System.NotImplementedException();
        }

        public void SignMessage(RSAParameters prikey, string haspro = "SHA256")
        {
            throw new System.NotImplementedException();
        }
    }
}