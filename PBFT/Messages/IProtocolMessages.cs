namespace PBFT.Messages
{
    //IProtocolMessages is an interface for our protocol message implementations.
    //The IProtocolMessages contains the necessary functions for an object implementation to act as a PBFT protocol message.
    public interface IProtocolMessages
    {
         public byte[] SerializeToBuffer(); //serialize the message object so that the object can be sent over the network.
    }
}