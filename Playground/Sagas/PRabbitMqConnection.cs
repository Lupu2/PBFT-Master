using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.Sagas
{
    public class PRabbitMqConnection : IPersistable
    {
        private readonly RabbitMqConnection _rabbitMqConnection;

        public PRabbitMqConnection(RabbitMqConnection rabbitMqConnection)
        {
            _rabbitMqConnection = rabbitMqConnection;
        }

        public void Receive(StartCommand cmd)
        {

        }

        public void Receive(FirstCommand cmd)
        {

        }

        public void Receive(SecondCommand cmd)
        {

        }

        public void Send(FirstCommand toSend)
        {

        }

        public void Send(SecondCommand toSend)
        {

        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            throw new NotImplementedException();
        }

        private static RabbitMqConnection Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            throw new NotImplementedException();
        }
    }
}
