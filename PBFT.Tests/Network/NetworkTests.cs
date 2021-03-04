using System;
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
    }
}