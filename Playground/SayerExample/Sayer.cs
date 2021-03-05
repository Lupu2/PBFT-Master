using System;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.SayerExample
{
    public class Sayer : IPropertyPersistable
    {
        public string Greeting { get; set; }

        public void Greet() => Console.WriteLine(Greeting);
    }
}