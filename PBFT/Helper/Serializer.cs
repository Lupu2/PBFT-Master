using System;
using System.Linq;
using Cleipnir.ObjectDB.PersistentDataStructures;
using PBFT.Certificates;

namespace PBFT.Helper
{
    
    public static class Serializer
    {
        //AddTypeIdentifierToBytes adds a bit value to the given byte array.
        //The bit chosen is based on the given MessageType enum.
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
                    resobj = copyobj.Concat(BitConverter.GetBytes(6)).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return resobj;
        }

        public static string SerializeHash(byte[] hash)
        {
            if (hash != null) return Convert.ToBase64String(hash);
            return null;
        }

        public static CList<ProtocolCertificate> PrepareForSerialize(CList<ProtocolCertificate> certs)
        {
            var copyList = new CList<ProtocolCertificate>();
            foreach (var cert in certs) copyList.Add(cert.CloneInfoCertificate());
            return copyList;
        }
    }
}