using System.Security.Cryptography;

namespace PBFT.ProtocolMessages
{
    public interface IProtocolMessages<T>
    {
         public byte[] SerializeToBuffer();
         public void SignMessage(RSAParameters prikey, string haspro="SHA256");
    }
}