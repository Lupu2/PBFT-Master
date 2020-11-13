using System;

namespace Playground
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HeartbeatSender.P.Do();

            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
