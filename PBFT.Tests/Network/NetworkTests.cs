using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;
using PBFT.Messages;
using PBFT.Network;

namespace PBFT.Tests.Network
{
    [TestClass]
    public class NetworkTests
    {
        [TestMethod]
        public void AddEndDelimiterTest()
        {
            var req = new Request(0, "no op");
            var phasemes = new PhaseMessage(0, 1, 1, Crypto.CreateDigest(req),PMessageType.PrePrepare);
            var orghash = phasemes.SerializeToBuffer();
            var addedbuff = NetworkFunctionality.AddEndDelimiter(orghash);
            Assert.IsTrue(addedbuff.Length > orghash.Length);
            var jsonobj = Encoding.ASCII.GetString(addedbuff);
            StringAssert.Contains(jsonobj, "|");
        }

        [TestMethod]
        public void HandlingTCPDuplicateProblemTest()
        {
            var (pri, pub) = Crypto.InitializeKeyPairs();
            var req = new Request(1,"op");
            var phasemes1 = new PhaseMessage(1, 1, 1, Crypto.CreateDigest(req), PMessageType.PrePrepare);
            var phasemes2 = new PhaseMessage(2, 1, 1, Crypto.CreateDigest(req), PMessageType.PrePrepare);
            phasemes1.SignMessage(pri);
            phasemes2.SignMessage(pri);
            var bytemes1 = NetworkFunctionality.AddEndDelimiter(
                Serializer.AddTypeIdentifierToBytes(
                    phasemes1.SerializeToBuffer(), 
                    MessageType.PhaseMessage)
                );
            var bytemes2 = NetworkFunctionality.AddEndDelimiter(
                Serializer.AddTypeIdentifierToBytes(
                    phasemes2.SerializeToBuffer(), 
                    MessageType.PhaseMessage)
            );
            var bytemes = bytemes1.Concat(bytemes2).ToArray();
            
            List<IProtocolMessages> incommingMessages = new List<IProtocolMessages>();
            List<int> types = new List<int>();
            var jsonstringobj = Encoding.ASCII.GetString(bytemes);
            var mesobjects = jsonstringobj.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (mesobjects.Length > 1)
            {
                int idx = 0;
                foreach (var mesjson in mesobjects)
                {
                    byte[] bytesegment = bytemes.ToArray();
                    if (idx != 0)
                    {
                        bytesegment = bytesegment
                            .Skip(idx+1)
                            .ToArray();
                    }
                    var messegment = bytesegment.Take(mesjson.Length).ToArray();
                    var (type, mes) = Deserializer.ChooseDeserialize(messegment);
                    types.Add(type);
                    incommingMessages.Add(mes);
                    idx = mesjson.Length;
                }
            }
            else
            {
                //Console.WriteLine(BitConverter.ToString(bytemes));
                var bytemesnodel = bytemes
                    .Take(bytemes.Length - 1)
                    .ToArray();
                //Console.WriteLine(Encoding.ASCII.GetString(bytemesnodel));
                var (mestype, mes) = Deserializer.ChooseDeserialize(bytemesnodel);
                types.Add(mestype);
                incommingMessages.Add(mes);
            }

            Assert.AreEqual(types.Count,2);
            Assert.AreEqual(types.Count, incommingMessages.Count);
            
            for (int i = 0; i < types.Count; i++)
            {
                Assert.IsTrue(Enums.ToEnumMessageType(types[i]).Equals(MessageType.PhaseMessage));
                var phasecopy = (PhaseMessage) incommingMessages[i];
                switch (i)
                {
                    case 0: 
                        Assert.IsTrue(phasecopy.Compare(phasemes1));
                        break;
                    case 1: 
                        Assert.IsTrue(phasecopy.Compare(phasemes2));
                        break;
                    default:
                        throw new ArgumentException("Not allowed");
                }
            }
        }
    }
}