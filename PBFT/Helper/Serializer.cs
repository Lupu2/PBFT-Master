using System;
using System.ComponentModel;
using System.Linq;

namespace PBFT.Helper
{
    public enum MessageType
    {
            SessionMessage = 0,
            Request = 1,
            PhaseMessage = 2,
            Reply = 3,
            ViewChange = 4,
            NewView = 5,
    }
    public static class Serializer
    {
        public static byte[] AddTypeIdentifierToBytes(byte[] sermes, MessageType type)
        {
            byte[] copyobj = sermes.ToArray();

            switch (type) 
            {
                case MessageType.SessionMessage:
                    copyobj.Concat(BitConverter.GetBytes(0));
                    break;
                case MessageType.Request:
                    copyobj.Concat(BitConverter.GetBytes(1));
                    break;
                case MessageType.PhaseMessage:
                    copyobj.Concat(BitConverter.GetBytes(2));
                    break;
                case MessageType.Reply:
                    copyobj.Concat(BitConverter.GetBytes(3));
                    break;
                case MessageType.ViewChange:
                    copyobj.Concat(BitConverter.GetBytes(4));
                    break;
                case MessageType.NewView:
                    copyobj.Concat(BitConverter.GetBytes(5));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return copyobj;
        }
    }
}