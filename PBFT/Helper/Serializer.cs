using System;
using System.ComponentModel;
using System.Linq;

namespace PBFT.Helper
{
    
    public static class Serializer
    {
        public static byte[] AddTypeIdentifierToBytes(byte[] sermes, MessageType type)
        {
            byte[] copyobj = sermes.ToArray();
            byte[] resobj;
            switch (type) 
            {
                case MessageType.SessionMessage:
                    resobj = copyobj.Concat(BitConverter.GetBytes(0000)).ToArray();
                    break;
                case MessageType.Request:
                    resobj = copyobj.Concat(BitConverter.GetBytes(0001)).ToArray();
                    break;
                case MessageType.PhaseMessage:
                    resobj = copyobj.Concat(BitConverter.GetBytes(0002)).ToArray();
                    break;
                case MessageType.Reply:
                    resobj = copyobj.Concat(BitConverter.GetBytes(3)).ToArray();
                    break;
                case MessageType.ViewChange:
                    resobj = copyobj.Concat(BitConverter.GetBytes(4)).ToArray();
                    break;
                case MessageType.NewView:
                    resobj = copyobj.Concat(BitConverter.GetBytes(5)).ToArray();
                    break;
                case MessageType.Checkpoint:
                //TODO insert serialization for Checkpoint
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return resobj;
        }

        public static string SerializeHash(byte[] hash) => System.Convert.ToBase64String(hash);
        
    }
}