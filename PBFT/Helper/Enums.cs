using System;
//using PBFT.Replica;
using PBFT.Messages;

namespace PBFT.Helper
{
    //Enums
    public enum CertType {
        Prepared,
        Committed,
        Reply,
        Checkpoint,
        ViewChange,
    }
    
    public enum PMessageType 
    {
        PrePrepare,
        Prepare,
        Commit,
    }
    
    public enum DeviceType
    {
        Client,
        Server,
    }
    
    //Transformations
    public static class Enums
    {
        public static DeviceType ToEnumDeviceType(int number)
        {
            if (Enum.IsDefined(typeof(PMessageType), number)) throw new InvalidOperationException();
            return (DeviceType) number;
        }
        
        public static PMessageType ToEnumPMessageType(int number)
        {
            if (Enum.IsDefined(typeof(PMessageType), number)) throw new InvalidOperationException();
            return (PMessageType) number;
        }
        
        public static CertType ToEnumCertType(int number)
        {
            if (Enum.IsDefined(typeof(CertType), number)) throw new InvalidOperationException();
            return (CertType) number;
        }
    }
}