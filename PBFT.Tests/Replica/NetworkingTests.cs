using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Schema;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Rx;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Replica;
namespace PBFT.Tests.Replica
{
    [TestClass]
    public class NetworkingTests
    {
        [TestMethod]
        public void SimpleListenerTest()
        {
            var serv = new Server(1, 1, 1, 4, null, 20, "127.0.0.1:9001", new Source<Request>(),
                new Source<PhaseMessage>(), new CDictionary<int, string>());
            serv.Start();
            new Thread(Sender) {IsBackground = true}.Start();
            Thread.Sleep(3000); //wait long enough for the server do its job, its stuck since it can't send back any messages
            Assert.IsTrue(serv.ClientPubKeyRegister.ContainsKey(1));
        }

        public void Sender()
        {
            var _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                while (!_socket.Connected)
                    _socket.Connect("127.0.0.1",9001);

                var ses = new SessionMessage(DeviceType.Client, new RSAParameters(), 1);
                var msg = ses.SerializeToBuffer();
                msg = Serializer.AddTypeIdentifierToBytes(msg, MessageType.SessionMessage);
                _socket.Send(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(3000);
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
        }

        /*[TestMethod]
        public void SimpleServerToServerCommunicationTest() //need to redesign network layer before continuing with these tests. Creating connection is not working properly atm.
        {
            var serv = new Server(0, 0, 0, 4, null, 20, "127.0.0.1:9000", new Source<Request>(), new Source<PhaseMessage>());
            serv.Start();
            new Thread(OtherServer) {IsBackground = true}.Start();
            Thread.Sleep(10000); //wait long enough for the server do its job, its stuck since it can't send back any messages
            Assert.IsTrue(serv.ServPubKeyRegister.ContainsKey(1));
        }

        public void OtherServer()
        {
            var serv = new Server(1, 0, 0, 4, null, 20, "127.0.0.1:9001", new Source<Request>(), new Source<PhaseMessage>());
            serv.Start();
            Dictionary<int, string> servers = new Dictionary<int, string>();
            servers.Add(0,"127.0.0.1:9000");
            serv.InitializeConnections(servers);
            Console.WriteLine("done");
            Thread.Sleep(5000);
        }*/
    }
}