using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
                    resobj = copyobj.Concat(BitConverter.GetBytes(0)).ToArray();
                    break;
                case MessageType.Request:
                    resobj = copyobj.Concat(BitConverter.GetBytes(1)).ToArray();
                    break;
                case MessageType.PhaseMessage:
                    resobj = copyobj.Concat(BitConverter.GetBytes(2)).ToArray();
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

        public static string SerializeHash(byte[] hash) => Convert.ToBase64String(hash);
        
    }
}