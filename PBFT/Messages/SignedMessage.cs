using System.Security.Cryptography;

namespace PBFT.Messages
{
    public interface SignedMessage
    {
         public void SignMessage(RSAParameters prikey, string haspro="SHA256");

         public IProtocolMessages CreateCopyTemplate();
    }
}