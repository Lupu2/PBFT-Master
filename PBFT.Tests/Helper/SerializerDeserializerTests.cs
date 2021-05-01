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
            var repmes = new Reply(1,1,1,1,true,"hello world", DateTime.Now.ToString());
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
            var viewmes = new ViewChange(1,1,1,null,new CDictionary<int, ProtocolCertificate>());
           var req = new Request(1, "12:00");
           var protocert = new ProtocolCertificate(1, 1,  Crypto.CreateDigest(req), CertType.Prepared);
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

           var checkpointCert = new CheckpointCertificate(2, Crypto.CreateDigest(req),
               delegate(CheckpointCertificate certificate) { });
           var viewmes2 = new ViewChange(2,2,2,checkpointCert,new CDictionary<int, ProtocolCertificate>());
           viewmes2.RemPreProofs[1] = new ProtocolCertificate(1, 1, Crypto.CreateDigest(req), CertType.Prepared);
           byte[] serpmes2 = viewmes2.SerializeToBuffer();
           byte[] readybuff2 = Serializer.AddTypeIdentifierToBytes(serpmes2, MessageType.ViewChange);
           Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff2)));
           var (mestype2,demes2) = Deserializer.ChooseDeserialize(readybuff2);
           Assert.IsTrue(mestype2 == 4);
           ViewChange viewmesde2 = (ViewChange) demes2;
           Console.WriteLine(viewmes2);
           Console.WriteLine(viewmesde2);
           Assert.IsTrue(viewmesde2.Compare(viewmes2));
        }
        
        [TestMethod]
        public void SerializeDeserializeNewView()
        {
            /*ViewPrimary info, CheckpointCertificate state, Action<ViewChangeCertificate> shutdown, Action viewchange*/
            ViewPrimary vp = new ViewPrimary(1, 1, 4);
            CList<PhaseMessage> preparemes = new CList<PhaseMessage>();
            ViewChangeCertificate viewproof = new ViewChangeCertificate(vp, null, null, null);
            var newviewmes = new NewView(1, viewproof, preparemes);
            byte[] serpmes = newviewmes.SerializeToBuffer();
            byte[] readybuff = Serializer.AddTypeIdentifierToBytes(serpmes, MessageType.NewView);
            Assert.IsFalse(BitConverter.ToString(serpmes).Equals(BitConverter.ToString(readybuff)));
            var (mestype,demes) = Deserializer.ChooseDeserialize(readybuff);
            Assert.IsTrue(mestype == 5);
            NewView newviewmesde = (NewView) demes;
            Console.WriteLine(newviewmes);
            Console.WriteLine(newviewmesde);
            Assert.IsTrue(newviewmes.Compare(newviewmesde));
            var dig = Crypto.CreateDigest(new Request(1, "12:00"));
            CheckpointCertificate check = new CheckpointCertificate(1, dig, null);
            ViewChangeCertificate viewproof2 = new ViewChangeCertificate(vp, check, null, null);
            preparemes.Add(new PhaseMessage(1,1,1,dig,PMessageType.PrePrepare));
            var newviewmes2 = new NewView(1, viewproof2, preparemes);
            byte[] serpmes2 = newviewmes2.SerializeToBuffer();
            byte[] readybuff2 = Serializer.AddTypeIdentifierToBytes(serpmes2, MessageType.NewView);
            Assert.IsFalse(BitConverter.ToString(serpmes2).Equals(BitConverter.ToString(readybuff2)));
            var (mestype2,demes2) = Deserializer.ChooseDeserialize(readybuff2);
            Assert.IsTrue(mestype2 == 5);
            NewView newviewmesde2 = (NewView) demes2;
            Console.WriteLine(newviewmes2);
            Console.WriteLine(newviewmesde2);
            Assert.IsTrue(newviewmes2.Compare(newviewmesde2));
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