using System;
using System.Linq;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Certificates;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;

namespace PBFT.Tests.Helper
{
    [TestClass]
    public class SerializerDeserializerTests
    {

        [TestMethod]
        public void SerializeDeserializeSessionMessage()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var sesmes = new Session(DeviceType.Client, pub,1);
            byte[] serpmes = sesmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.SessionMessage);
            Assert.IsFalse(serpmes.SequenceEqual(readybuff));
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 0);
            Session sesmede = (Session) demes;
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
           var viewmes = new ViewChange(1,1,1,null,new CDictionary<int, ProtocolCertificate>());
           var req = new Request(1, "12:00");
           var protocert = new ProtocolCertificate(1, 1,  Crypto.CreateDigest(new Request(1, "12:00")), CertType.Prepared);
           viewmes.RemPreProofs[1] = protocert;
           byte[] serpmes = viewmes.SerializeToBuffer();
           byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.ViewChange);
           Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
           var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
           Assert.IsTrue(mestype == 4);
           ViewChange viewmesde = (ViewChange) demes;
           Console.WriteLine(viewmes);
           Console.WriteLine(viewmesde);
           Assert.IsTrue(viewmes.Compare(viewmesde));
        }
        
        [TestMethod]
        public void SerializeDeserializeNewView()
        {
            /*ViewPrimary info, CheckpointCertificate state, Action<ViewChangeCertificate> shutdown, Action viewchange*/
            
            //TODO Write test after finishing code for NewView
            ViewPrimary vp = new ViewPrimary(1, 1, 4);
            CList<PhaseMessage> preparemes = new CList<PhaseMessage>();
            ViewChangeCertificate viewproof = new ViewChangeCertificate(vp, null, null, null);
            var newviewmes = new NewView(1, viewproof, preparemes);
            byte[] serpmes = newviewmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.NewView);
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 4);
            NewView newviewmesde = (NewView) demes;
            Console.WriteLine(newviewmes);
            Console.WriteLine(newviewmesde);
            Assert.IsTrue(newviewmes.Compare(newviewmesde));
        }
        
        [TestMethod]
        public void SerializeDeserializeCheckpoint()
        {
            var checkmes = new Checkpoint(1, 10, null);
            byte[] serpmes = checkmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.Checkpoint);
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 6);
            Checkpoint checkde = (Checkpoint) demes;
            Assert.IsTrue(checkmes.Compare(checkde));
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