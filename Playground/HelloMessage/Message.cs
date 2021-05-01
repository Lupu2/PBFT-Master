using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;


namespace Playground.HelloMessage
{

    public enum subtype
    {
        Hello,
        Response
    }
    public class Message : IPersistable
    {
        public string mes{get; set;}
        public string color{get; set;}

        //public subtype type{get; set;}
        public int type {get; set;}
        public int posx{get; set;}
        public int posy{get; set;}

        public Message(string me, string col, int st, int x, int y)
        {
            mes = me;
            color = col;
            type = st;
            posx = x;
            posy = y;
        }

        public override string ToString() => $"Message:{mes}, Color:{color}, PosX:{posx}, PosY:{posy}";

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            stateToSerialize.Set(nameof(mes), mes);
            stateToSerialize.Set(nameof(color), color);
            stateToSerialize.Set(nameof(type), type);
            stateToSerialize.Set(nameof(posx), posx);
            stateToSerialize.Set(nameof(posy), posy);
        }

        private static Message Deserialize(IReadOnlyDictionary<string, object> sd)
        => new Message((string) sd[nameof(mes)],
                (string) sd[nameof(color)],
                //(subtype) sd[nameof(type)],
                (int) sd[nameof(type)],
                (int) sd[nameof(posx)],
                (int) sd[nameof(posy)]);
        

    }
}