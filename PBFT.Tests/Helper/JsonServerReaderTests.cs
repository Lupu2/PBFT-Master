using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PBFT.Helper;

namespace PBFT.Tests
{
    [TestClass]
    public class JsonServerReaderTests
    {
        //Assume your running code in Rider

        [TestInitialize]
        public void Initializer()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            if (!Directory.GetCurrentDirectory()
                .Equals(@"C:\Users\jorge\Documents\uis_10th_semester\githubrepos\NewRepository\PBFT-Master\PBFT"))
                Directory.SetCurrentDirectory("../../../../PBFT");
        }
        
        [TestMethod]
        public void JsonLoadServerData()
        {
            var serverdata = LoadJSONValues.GetServerData("testServerInfo.json", 0).GetAwaiter().GetResult();
            var id = serverdata.Item1;
            var ipaddr = serverdata.Item2;
            Console.WriteLine(id);
            Console.WriteLine(ipaddr);
            StringAssert.Contains(ipaddr, "127.0.0.1:9000");
            Assert.AreEqual(id,0);
        }

        [TestMethod]
        public void JsonLoadServerFileContent()
        {
            var filecontent = LoadJSONValues.LoadJSONFileContent("testServerInfo.json").GetAwaiter().GetResult();
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
        
    }
}