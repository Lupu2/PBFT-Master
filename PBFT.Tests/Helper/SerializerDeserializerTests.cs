using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;

namespace PBFT.Tests.Helper
{
    [TestClass]
    public class SerializerDeserializerTests
    {

        [TestMethod]
        public void SerializeDeserializeSessionMessage()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var sesmes = new SessionMessage(DeviceType.Client, pub,1);
            byte[] serpmes = sesmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.SessionMessage);
            Assert.IsFalse(serpmes.SequenceEqual(readybuff));
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 0);
            SessionMessage sesmede = (SessionMessage) demes;
            Assert.IsTrue(sesmes.Compare(sesmede));
        }

        [TestMethod]
        public void SerializeDeserializeRequest()
        {
            var reqmes = new Request(1,"Hello World", DateTime.Now.ToString());
            byte[] serpmes = reqmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.Request);
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 1);
            Request reqmesde = (Request) demes;
            Assert.IsTrue(reqmes.Compare(reqmesde));
        }
        
        [TestMethod]
        public void SerializeDeserializePhaseMessage()
        {
            var digest = Crypto.CreateDigest(new Request(1, "hello world", DateTime.Now.ToString()));
            var pmes = new PhaseMessage(1, 1, 1, digest, PMessageType.PrePrepare);
            byte[] serpmes = pmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.PhaseMessage);
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 2);
            PhaseMessage pmesde = (PhaseMessage) demes;
            Assert.IsTrue(pmes.Compare(pmesde));
        }
        
        [TestMethod]
        public void SerializeDeserializeReply()
        {
            var repmes = new Reply(1,1,1,true,"hello world", DateTime.Now.ToString());
            byte[] serpmes = repmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.Reply);
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 3);
            Reply repmesde = (Reply) demes;
            Console.WriteLine(repmes);
            Console.WriteLine(repmesde);
            Assert.IsTrue(repmes.Compare(repmesde));
        }
        
        [TestMethod]
        public void SerializeDeserializeViewChange()
        {
           //TODO implement after finishing the code for ViewChange struct
        }
        
        [TestMethod]
        public void SerializeDeserializeNewView()
        {
            //TODO Write test after finishing code for NewView
        }
        
        [TestMethod]
        public void SerializeDeserializeCheckpoint()
        {
            //TODO Write test after finishing code for Checkpoint
        }
        
        [TestMethod]
        public void TestSerializerException()
        {
            byte[] test = {1,2,3};
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Serializer.AddTypeIdentifierToBytes(test,(MessageType)7));
        }

        [TestMethod]
        public void TestDeserializerException()
        {
            byte[] test = {1, 2, 3};
            var mes = Assert.ThrowsException<IndexOutOfRangeException>(() => Deserializer.ChooseDeserialize(test));
            StringAssert.Contains(mes.Message, "INVALID INPUT ARGUMENT");
            byte[] test2 = {1, 2, 3, 4, 5};
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Deserializer.ChooseDeserialize(test2));
        }
    }
}