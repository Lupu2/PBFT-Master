using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using Cleipnir.Rx;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using MessagePack;

namespace Playground.HelloMessage
{
    public class Hello : IPersistable
    {
        public Source<Message> subject {get; set;}
        public int id{get; set;}

        public Message GivenMessage{get; set;}

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(subject), subject);
            stateToSerialize.Set(nameof(id), id);
            stateToSerialize.Set(nameof(GivenMessage),GivenMessage);
        }

        private static Hello Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            return new Hello()
            {
                id = (int) sd[nameof(id)],
                subject = (Source<Message>) sd[nameof(subject)],
                GivenMessage = (Message) sd[nameof(GivenMessage)]
            };
        }

        public async CTask Start()
        {
            while (true)
            {
                
                await Sleep.Until(1000);
                //Console.WriteLine($"Hello turn, Write a message Hello{id}");
                Console.WriteLine("Player: " + this.ToString() + " turn!");
                //string mes = Console.ReadLine();
                //string mes = "Hello";
                //Message ms = new Message(mes,"blue",subtype.Hello,0,1);
                subject.Emit(GivenMessage);
                Console.WriteLine("Emitted message");
                var resp = await subject.Where(m => m.type == 1).Next();
                Console.WriteLine("Response received: " + resp.ToString());
            }
        }

        public async CTask Responder()
        {
            while (true)
            {
                //Console.WriteLine("Waiting for hellos!");
                var hello = await subject.Where(m => m.type.Equals(0)).Next();
                Console.WriteLine("Received Hello: " + hello.ToString());
                //Console.WriteLine($"Responder turn, Write a message responder{id}");
                Console.WriteLine("Player: " + this.ToString() + " turn!");
                //string resp = Console.ReadLine();
                //string resp = "response!";
                //Message ms = new Message(resp,"red",subtype.Response,1,0);
                await Sleep.Until(1000);
                subject.Emit(GivenMessage);
            }
        }

        public override string ToString() => $"ID: {id}, Message: {GivenMessage}";
    }
}