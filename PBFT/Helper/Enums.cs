using System;

namespace PBFT.Helper
{
    //Enums
    public enum CertType {
        Prepared = 0,
        Committed = 1,
        Reply = 2,
        ViewChange = 3,
        Checkpoint = 4,
    }
    
    public enum DeviceType
    {
        Client = 0,
        Server = 1,
    }
    
    public enum PMessageType 
    {
        PrePrepare = 0,
        Prepare = 1,
        Commit = 2,
        End = 3,
    }
    
    public enum MessageType
    {
        SessionMessage = 0,
        Request = 1,
        PhaseMessage = 2,
        Reply = 3,
        ViewChange = 4,
        NewView = 5,
        Checkpoint = 6,
    }
    
    //Transformations
    public static class Enums
    {
        //ToEnumCertType converts the given number into the appropriate CertType enum based on its value.
        //Returns an ArgumentOutOfRangeException if the value is within the CertType enums range.
        public static CertType ToEnumCertType(int number)
        {
            if (!Enum.IsDefined(typeof(CertType), number)) throw new ArgumentOutOfRangeException();
            return (CertType) number;
        }
        
        //ToEnumDeviceType converts the given number into the appropriate DeviceType enum based on its value.
        //Returns an ArgumentOutOfRangeException if the value is within the DeviceType enums range.
        public static DeviceType ToEnumDeviceType(int number)
        {
            if (!Enum.IsDefined(typeof(DeviceType), number)) throw new ArgumentOutOfRangeException();
            return (DeviceType) number;
        }
        
        //ToEnumPMessageType converts the given number into the appropriate PMessageType enum based on its value.
        //Returns an ArgumentOutOfRangeException if the value is within the PMessageType enums range.
        public static PMessageType ToEnumPMessageType(int number)
        {
            if (!Enum.IsDefined(typeof(PMessageType), number)) throw new ArgumentOutOfRangeException();
            return (PMessageType) number;
        }
        
        //ToEnumMessageType converts the given number into the appropriate MessageType enum based on its value.
        //Returns an ArgumentOutOfRangeException if the value is within the MessageType enums range.
        public static MessageType ToEnumMessageType(int number)
        {
            if (!Enum.IsDefined(typeof(MessageType), number)) throw new ArgumentOutOfRangeException();
            return (MessageType) number;
        }
    }
}