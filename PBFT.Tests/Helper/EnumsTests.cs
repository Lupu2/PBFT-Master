using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;

namespace PBFT.Tests
{
    [TestClass]
    public class EnumsTests
    {
        [TestMethod]
        public void CertTypeTest()
        {
            Assert.AreEqual(CertType.Prepared, Enums.ToEnumCertType(0));
            Assert.AreEqual(CertType.Committed, Enums.ToEnumCertType(1));
            Assert.AreEqual(CertType.Reply, Enums.ToEnumCertType(2));
            Assert.AreEqual(CertType.ViewChange, Enums.ToEnumCertType(3));
            Assert.AreEqual(CertType.Checkpoint, Enums.ToEnumCertType(4));
            Assert.AreNotEqual(CertType.Prepared, Enums.ToEnumCertType(1));
            Assert.AreNotEqual(PMessageType.Prepare, Enums.ToEnumCertType(1));
            Assert.AreNotEqual(PMessageType.Prepare, Enums.ToEnumCertType(0));
            Assert.AreNotEqual(CertType.Committed, Enums.ToEnumCertType(4));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Enums.ToEnumCertType(5));
        }
        
        [TestMethod]
        public void DeviceTypeTest()
        {
            Assert.AreEqual(DeviceType.Client,Enums.ToEnumDeviceType(0));
            Assert.AreEqual(DeviceType.Server, Enums.ToEnumDeviceType(1));
            Assert.AreNotEqual(DeviceType.Client, Enums.ToEnumDeviceType(1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Enums.ToEnumDeviceType(4));
        }

        [TestMethod]
        public void PMessageTypeTest()
        {
            Assert.AreEqual(PMessageType.PrePrepare,Enums.ToEnumPMessageType(0));
            Assert.AreEqual(PMessageType.Prepare, Enums.ToEnumPMessageType(1));
            Assert.AreEqual(PMessageType.Commit, Enums.ToEnumPMessageType(2));
            Assert.AreNotEqual(PMessageType.PrePrepare, Enums.ToEnumPMessageType(1));
            Assert.AreNotEqual(PMessageType.Commit, Enums.ToEnumPMessageType(1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Enums.ToEnumPMessageType(4));
        }
        
        [TestMethod]
        public void MessageTypeTest()
        {
            Assert.AreEqual(MessageType.SessionMessage, Enums.ToEnumMessageType(0));
            Assert.AreEqual(MessageType.Request, Enums.ToEnumMessageType(1));
            Assert.AreEqual(MessageType.PhaseMessage, Enums.ToEnumMessageType(2));
            Assert.AreEqual(MessageType.Reply, Enums.ToEnumMessageType(3));
            Assert.AreEqual(MessageType.ViewChange, Enums.ToEnumMessageType(4));
            Assert.AreEqual(MessageType.NewView, Enums.ToEnumMessageType(5));
            Assert.AreEqual(MessageType.Checkpoint, Enums.ToEnumMessageType(6));
            Assert.AreNotEqual(MessageType.Reply,  Enums.ToEnumMessageType(4));
            Assert.AreNotEqual(CertType.Reply, Enums.ToEnumMessageType(3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Enums.ToEnumMessageType(7));
        }
    }
}