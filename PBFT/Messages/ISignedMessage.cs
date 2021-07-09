using System.Security.Cryptography;

namespace PBFT.Messages
{
    //IProtocolMessages is an interface for our protocol message implementations.
    //The IQProtocolMessages contains the functions required for a message object to use signature functionality.
    public interface ISignedMessage
    {
         public void SignMessage(RSAParameters prikey, string haspro="SHA256");

         public IProtocolMessages CreateCopyTemplate();
    }
}