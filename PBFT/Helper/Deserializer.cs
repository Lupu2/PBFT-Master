using System;
using System.Linq;
using PBFT.Network;
using PBFT.Messages;

namespace PBFT.Helper
{
    public static class Deserializer 
    {
        public static (int, IProtocolMessages) ChooseDeserialize(byte[] sermessage)
        {
            int formatByte = sermessage[sermessage.Length-1];
            byte[] serobj = sermessage.Take(sermessage.Length-1).ToArray();
            switch (formatByte) 
            {
                case (int) MessageType.SessionMessage:
                     return (formatByte, SessionMessage.DeSerializeToObject(serobj));
                case (int) MessageType.Request:
                    return (formatByte, Request.DeSerializeToObject(serobj));
                case (int) MessageType.PhaseMessage:
                    return (formatByte, PhaseMessage.DeSerializeToObject(serobj));
                case (int) MessageType.Reply:
                    return (formatByte, Reply.DeSerializeToObject(serobj));
                case (int) MessageType.ViewChange:
                    return (formatByte, ViewChange.DeSerializeToObject(serobj));
                case (int) MessageType.NewView:
                    return (formatByte, NewView.DeSerializeToObject(serobj));
                case (int) MessageType.Checkpoint:
                    //TODO insert deserialization for Checkpoint
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public static byte[] DeserializeHash(string hashstring) => Convert.FromBase64String(hashstring);
    }
}