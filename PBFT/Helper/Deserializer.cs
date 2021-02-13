using System;
using System.Linq;
using PBFT.Network;
using PBFT.Messages;

namespace PBFT.Helper
{
    public static class Deserializer 
    {
        public static IProtocolMessages ChooseDeserialize(byte[] sermessage)
        {
            int formatByte = sermessage[sermessage.Length-1];
            byte[] serobj = sermessage.Take(sermessage.Length-1).ToArray();
            switch (formatByte) 
            {
                case (int) MessageType.SessionMessage:
                     return SessionMessage.DeSerializeToObject(serobj);
                case (int) MessageType.Request:
                    return Request.DeSerializeToObject(serobj);
                case (int) MessageType.PhaseMessage:
                    return PhaseMessage.DeSerializeToObject(serobj);
                case (int) MessageType.Reply:
                    return Reply.DeSerializeToObject(serobj);
                case (int) MessageType.ViewChange:
                    return ViewChange.DeSerializeToObject(serobj);
                case (int) MessageType.NewView:
                    return NewView.DeSerializeToObject(serobj);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public static byte[] DeserializeHash(string hashstring) => Convert.FromBase64String(hashstring);
    }
}