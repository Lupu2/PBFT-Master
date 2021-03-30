using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Network;
using PBFT.Replica;

namespace PBFT.Tests.Replica
{
    [TestClass]
    public class NetworkingTests
    {
        [TestMethod]
        public void SimpleListenerTest()
        {
            var sh = new SourceHandler(new Source<Request>(), new Source<PhaseMessage>(), null, null, null, null);
            var serv = new Server(1, 1, 4, null, 20, "127.0.0.1:9001", sh, new CDictionary<int, string>());
            serv.Start();
            new Thread(Sender) {IsBackground = true}.Start();
            Thread.Sleep(5000); //wait long enough for the server do its job, its stuck since it can't send back any messages
            Assert.IsTrue(serv.ClientPubKeyRegister.ContainsKey(1));
            serv.Dispose();
        }

        public void Sender()
        {
            var _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                while (!_socket.Connected)
                    _socket.Connect("127.0.0.1",9001);

                var ses = new Session(DeviceType.Client, new RSAParameters(), 1);
                var msg = ses.SerializeToBuffer();
                msg = Serializer.AddTypeIdentifierToBytes(msg, MessageType.SessionMessage);
                msg = NetworkFunctionality.AddEndDelimiter(msg);
                _socket.Send(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(3000);
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
        }

        [TestMethod]
        public void SimpleServerToServerCommunicationTest() //need to redesign network layer before continuing with these tests. Creating connection is not working properly atm.
        {
            var sourceHandler = new SourceHandler(new Source<Request>(), new Source<PhaseMessage>(), null, null, null, null);
            var serv = new Server(0, 0, 4, null, 20, "127.0.0.1:9000", sourceHandler, new CDictionary<int, string>());
            serv.ServerContactList[0] = "127.0.0.1:9000";
            serv.Start();
            new Thread(()=> OtherServer(serv.Pubkey)) {IsBackground = true}.Start();
            Thread.Sleep(2500); //wait long enough for the server do its job, its stuck since it can't send back any messages
            Assert.IsTrue(serv.ServPubKeyRegister.ContainsKey(1));
            
            serv.Dispose();
        }

        public void OtherServer(RSAParameters otherpubkey)
        {
            var sh = new SourceHandler(new Source<Request>(), new Source<PhaseMessage>(), null, null, null, null);
            CDictionary<int, string> servers = new CDictionary<int, string>();
            servers[0] = "127.0.0.1:9000";
            var serv = new Server(1, 0, 4, null, 20, "127.0.0.1:9001", sh, servers);
            serv.Start();
            serv.InitializeConnections();
            Thread.Sleep(2500);
            Assert.IsTrue(serv.ServPubKeyRegister.ContainsKey(0));
            Assert.IsTrue(serv.ServPubKeyRegister[0].Exponent.SequenceEqual(otherpubkey.Exponent));
            Assert.IsTrue(serv.ServPubKeyRegister[0].Modulus.SequenceEqual(otherpubkey.Modulus));
        }

        [TestMethod]
        public void SimpleServerConnectionPhaseMessageTest()
        {
            var mesSource = new Source<PhaseMessage>();
            var sh = new SourceHandler(new Source<Request>(), mesSource, null, null, null, null);
            var serv = new Server(0, 0, 4, null, 20, "127.0.0.1:9000", sh, new CDictionary<int, string>());
            serv.ServerContactList[0] = "127.0.0.1:9000";
            serv.Start();
            new Thread(OtherServerPhase) {IsBackground = true}.Start();
            Thread.Sleep(3000); //wait long enough for the server do its job, its stuck since it can't send back any messages
            Assert.IsTrue(serv.ServPubKeyRegister.ContainsKey(1));
            var pesmes = ListenForMessage(mesSource).Result;
            Console.WriteLine("Got PhaseMessage");
            Console.WriteLine(pesmes);
            Assert.AreEqual(pesmes.ServID,1);
            Assert.AreEqual(pesmes.SeqNr,0);
            Assert.AreEqual(pesmes.ViewNr,0);
            Assert.AreEqual(pesmes.Digest,null);
            serv.Dispose();
        }

        public async Task<PhaseMessage> ListenForMessage(Source<PhaseMessage> messource)
        {
            var phmes = await messource.Next();
            return phmes;
        }

        public void OtherServerPhase()
        {
            var sh = new SourceHandler(new Source<Request>(), new Source<PhaseMessage>(), null, null, null, null);
            CDictionary<int, string> servers = new CDictionary<int, string>();
            servers[0] = "127.0.0.1:9000";
            var serv = new Server(1, 0, 4, null, 20, "127.0.0.1:9001", sh, servers);
            serv.Start();
            serv.InitializeConnections();
            Thread.Sleep(3000);
            Assert.IsTrue(serv.ServPubKeyRegister.ContainsKey(0));
            PhaseMessage pmes = new PhaseMessage(serv.ServID, 0, 0, null, PMessageType.PrePrepare);
            serv.SendMessage(pmes.SerializeToBuffer(), serv.ServConnInfo[0].Socket, MessageType.PhaseMessage);
            //Console.WriteLine("PhaseMessage Sent");
            //Thread.Sleep(8000);
        }

        /*[TestMethod]
        public void SimpleClientRequestTest()
        {
            var reqSource = new Source<Request>();
            var serv = new Server(0, 0, 4, null, 20, "127.0.0.1:9000",  reqSource, new Source<PhaseMessage>(), new CDictionary<int, string>());
            serv.ServerContactList[0] = "127.0.0.1:9000";
            serv.Start();
            new Thread(PseudoClient) {IsBackground = true}.Start();
            //Thread.Sleep(3000); //wait long enough for the server do its job, its stuck since it can't send back any messages
            //Assert.IsTrue(serv.ClientPubKeyRegister.ContainsKey(1));
            var pesmes = ListenForMessage(reqSource).Result;
            Console.WriteLine("Got PhaseMessage");
            Console.WriteLine(pesmes);
            Assert.AreEqual(pesmes.ClientID, 1);
            StringAssert.Contains(pesmes.Message, "Hello Everybody!");
        }*/
        
        public async Task<Request> ListenForMessage(Source<Request> reqsource)
        {
            var phmes = await reqsource.Next();
            return phmes;
        }
        
        public void PseudoClient()
        {
            var _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                while (!_socket.Connected)
                    _socket.Connect("127.0.0.1", 9000);
                var (_pri, pub) = Crypto.InitializeKeyPairs();
                var ses = new Session(DeviceType.Client, pub, 1);
                var msg = ses.SerializeToBuffer();
                msg = Serializer.AddTypeIdentifierToBytes(msg, MessageType.SessionMessage);
                _socket.Send(msg);
                var buffer = new byte[1024];
                var msgLength = _socket.Receive(buffer, SocketFlags.None);
                var bytemes = buffer
                    .ToList()
                    .Take(msgLength)
                    .ToArray();
                var (_, mes) = Deserializer.ChooseDeserialize(bytemes);
                Session sesmes = (Session) mes;
                Console.WriteLine("CLIENT RECEIVED SESSION MESSAGE");
                Assert.AreEqual(sesmes.DevID, 0);
                Request req = new Request(1, "Hello Everybody!");
                req.SignMessage(_pri);
                var reqbuff = Serializer.AddTypeIdentifierToBytes(req.SerializeToBuffer(), MessageType.Request);
                _socket.Send(reqbuff);
                Console.WriteLine("CLIENT SENT REQUEST");
                //Thread.Sleep(5000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exeception");
                Console.WriteLine(e);
                Thread.Sleep(3000);
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
        }
            
        
    }
}