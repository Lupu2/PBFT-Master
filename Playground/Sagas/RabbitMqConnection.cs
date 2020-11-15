using System;
using System.Collections.Generic;
using System.Text;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.Sagas
{
    public class RabbitMqConnection : IPersistable
    {
        private readonly Cleipnir.Saga.Sagas _sagas;

        public RabbitMqConnection(Cleipnir.Saga.Sagas sagas)
        {
            _sagas = sagas;
        }

        public void Receive(StartCommand cmd)
        {
            _sagas.Deliver(cmd, cmd.InstanceId, cmd.GroupId, msg => { 
                return new TwoStepSaga() {Messages = msg, RabbitMq = new PRabbitMqConnection(this)}; });
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

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper)
        {
            throw new NotImplementedException();
        }

        private static RabbitMqConnection Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            throw new NotImplementedException();
        }
    }
}
