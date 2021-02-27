using System;
using PBFT.Client;

namespace PBFTClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int paramid = Int32.Parse(args[0].Split("id=")[1]);
            bool testparam = Boolean.Parse(args[1].Split("test=")[1]);
            Client cli = new Client(paramid);
            if (testparam) cli.LoadServerInfo("../PBFT/testServerInfo.json");
            else cli.LoadServerInfo("../PBFT/testServerInfo.json");
            _ = cli.InitializeConnections();
            
        }
    }
}
