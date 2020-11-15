using System;
using System.Reflection;

namespace Playground
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var fullName = executingAssembly.FullName;
            Console.WriteLine(fullName);

            Console.WriteLine("PRESS ENTER TO EXIT");
            Console.ReadLine();
        }
    }
}
