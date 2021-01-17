using System;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.Saga;

namespace Playground.Sagas
{
    internal class TwoStepSaga : IPropertyPersistable
    {
        public Messages Messages { get; set; }
        public PRabbitMqConnection RabbitMq { get; init; }

        public async CTask Start()
        {
            var cmd = await Messages.OfType<StartCommand>().Next();
            Console.WriteLine($"Starting Command with id: {cmd.InstanceId} and message: {cmd.CommandMsg}");
        }
    }
}
