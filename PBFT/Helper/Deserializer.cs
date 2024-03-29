using System;
using System.Linq;
using PBFT.Messages;

namespace PBFT.Helper
{
    public static class Deserializer 
    {
        //ChooseDeserialize takes the given bytes and deserialize it to appropriate message object.
        //The desrialization used is based on the type bit set by the serialization process.
        public static (int, IProtocolMessages) ChooseDeserialize(byte[] sermessage)
        {
            if (sermessage.Length < 4)
                throw new IndexOutOfRangeException("INVALID INPUT ARGUMENT");
            
            //Collect the last 4bytes to get MessageType value
            int formatByte = BitConverter.ToInt32(sermessage.Reverse()
                                                               .Take(4)
                                                               .Reverse()
                                                               .ToArray()
            );
            byte[] serobj = sermessage.Take(sermessage.Length-4)
                                      .ToArray();
            switch (formatByte) 
            {
                case (int) MessageType.SessionMessage:
                     return (formatByte, Session.DeSerializeToObject(serobj));
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
                    return (formatByte, Checkpoint.DeSerializeToObject(serobj));
                default:
                    Console.WriteLine("Illegal format for deserializer");
                    throw new ArgumentOutOfRangeException(); 
            }
        }

        //DeserializeHash reverts the serialization process we used for the given signature.
        public static byte[] DeserializeHash(string hashstring)
        {
            if (hashstring != null) return Convert.FromBase64String(hashstring);
            return null;
        }
    }
}