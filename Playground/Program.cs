using System;
using static Playground.SingleMachinePingPong.StorageEngineImplementation;

namespace Playground
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            SingleMachinePingPong.P.StartNew(File);
            
            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
