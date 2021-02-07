using System;
using static Playground.SingleMachinePingPong.StorageEngineImplementation;

namespace Playground
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            //SingleMachinePingPong.P.StartNew(File);
            //HeartbeatSender.P.Do();
            //SayerExample.P.Do();
            //TravelAgent.P.Do();
            //SqlExample.P.Do();
            //HelloMessage.P.DO();
            
            //SimulateSendReceive.P.Do();
            //MySqlStorageEngine.P.Do();
            PoormansSchedulerOld.P.Do();
            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
