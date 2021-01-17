using System;
using Cleipnir.ObjectDB;

namespace Cleipnir.Saga
{
    public class Sagas
    {
        private readonly ISagaStorageEngine _storageEngine;
        private readonly int _atVersion;

        public Sagas(ISagaStorageEngine storageEngine, int atVersion)
        {
            _storageEngine = storageEngine;
            _atVersion = atVersion;
        }

        public void Deliver<TSaga>(object msg, string instanceId, string groupId, Func<Messages, TSaga> sagaFactory, params object[] ephemeralInstances)
        {
            var storageEngine = _storageEngine.GetStorageEngine(instanceId, groupId);
            ObjectStore os;
            if (!storageEngine.Exist)
            {
                if (_storageEngine.AtVersion(groupId) != _atVersion) return; //new version of the saga exists which should handle new sagas

                os = ObjectStore.New(storageEngine);
                var messages = new Messages();
                var saga = sagaFactory(messages);
                os.Attach(messages);
                os.Attach(saga);
            } else
                os = ObjectStore.Load(storageEngine, ephemeralInstances);

            os.Resolve<Messages>().Emit(msg);

            os.Persist();
        }
    }
}
