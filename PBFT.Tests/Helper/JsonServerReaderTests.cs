using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;

namespace PBFT.Tests.Helper
{
    [TestClass]
    public class JsonServerReaderTests
    {
        //Current implementation assumes the test is run by Rider

        [TestInitialize]
        public void Initializer()
        {   //Update in order to work with your own directory!
            Console.WriteLine(Directory.GetCurrentDirectory());
            if (!Directory.GetCurrentDirectory()
                .Equals(@"C:\Users\jorge\Documents\uis_10th_semester\githubrepos\FinalDirectory\PBFT-Master\PBFT"))
                Directory.SetCurrentDirectory("../../../../PBFT");
        }
        
        [TestMethod]
        public void JsonLoadTestServerData()
        {
            var serverdata = LoadJSONValues.GetServerData("JSONFiles/testServerInfo.json", 0).GetAwaiter().GetResult();
            var id = serverdata.Item1;
            var ipaddr = serverdata.Item2;
            Console.WriteLine(id);
            Console.WriteLine(ipaddr);
            StringAssert.Contains(ipaddr, "127.0.0.1:9000");
            Assert.AreEqual(id,0);
        }

        [TestMethod]
        public void JsonLoadTestServerFileContent()
        {
            var filecontent = LoadJSONValues.LoadJSONFileContent("JSONFiles/testServerInfo.json").GetAwaiter().GetResult();
            int i = 0;
            string baseIP = "127.0.0.1:900";
            foreach (var (id,ip) in filecontent)
            {
                Assert.AreEqual(id, i);
                string expIP = baseIP + i;
                StringAssert.Contains(ip,expIP);
                i++;
            }
        }
        
        [TestMethod]
        public void JsonLoadServerData()
        {
            var serverdata = LoadJSONValues.GetServerData("JSONFiles/serverInfo.json", 0).GetAwaiter().GetResult();
            var id = serverdata.Item1;
            var ipaddr = serverdata.Item2;
            Console.WriteLine(id);
            Console.WriteLine(ipaddr);
            StringAssert.Contains(ipaddr, "192.168.2.0:9000");
            Assert.AreEqual(id,0);
        }

        [TestMethod]
        public void JsonLoadServerFileContent()
        {
            var filecontent = LoadJSONValues.LoadJSONFileContent("JSONFiles/serverInfo.json").GetAwaiter().GetResult();
            int i = 0;
            string baseIP = "192.168.2";
            string portNr = "9000";
            foreach (var (id,ip) in filecontent)
            {
                Assert.AreEqual(id, i);
                string expIP = baseIP + "." + i + ":" + portNr;
                StringAssert.Contains(ip,expIP);
                i++;
            }
        }
    }
}