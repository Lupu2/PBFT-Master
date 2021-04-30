using System;
using System.IO;

namespace PBFTClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //Format id=id test=true/false
            int paramid = Int32.Parse(args[0].Split("id=")[1]);
            bool testparam = Boolean.Parse(args[1].Split("test=")[1]);
            Client cli = new Client(paramid);
            if (testparam)
            {
                Directory.SetCurrentDirectory(@"C:\Users\jorge\Documents\uis_10th_semester\githubrepos\NewRepository\PBFT-Master\PBFTClient");
                cli.LoadServerInfo("JSONFiles/testServerInfo.json");
            }
            else cli.LoadServerInfo("JSONFiles/serverInfo.json");
            cli.SetFNumber();
            var connections = cli.InitializeConnections();
            connections.Wait();
            cli.ClientOperation();
        }
    }
}
