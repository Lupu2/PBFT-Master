using System;
using PBFT.Server;
using PBFT.Messages;

namespace PBFT.Helper
{
    public static class EnumTransformer
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