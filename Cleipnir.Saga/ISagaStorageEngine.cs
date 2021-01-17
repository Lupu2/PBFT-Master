using Cleipnir.StorageEngine;

namespace Cleipnir.Saga
{
    public interface ISagaStorageEngine
    {
        public int AtVersion(string groupId);

        public IStorageEngine GetStorageEngine(string instanceId, string group);
    }
}
