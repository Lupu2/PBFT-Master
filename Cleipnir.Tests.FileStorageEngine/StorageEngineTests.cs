using System.Collections.Generic;
using System.Linq;
using Cleipnir.StorageEngine;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.FileStorageEngine
{
    [TestClass]
    public class StorageEngineTests
    {
        [TestMethod]
        public void StoredEntriesCanBeLoadedAgain()
        {
            var fileStorage = new SimpleFileStorageEngine("./test.txt", true);
            var storageEntries = new List<StorageEntry>()
            {
                new StorageEntry(0, "someKey", 1),
                new StorageEntry(0, "someKey2", "someValue")
            };
            
            fileStorage.Persist(new DetectedChanges(
                storageEntries, 
                new List<ObjectIdAndKey>())
            );
            
            fileStorage.Dispose();
            
            fileStorage = new SimpleFileStorageEngine("./test.txt", false);
            var loadedEntries = fileStorage.Load().ToArray();
            loadedEntries.Length.ShouldBe(2);
            loadedEntries[0].Key.ShouldBe("someKey");
            loadedEntries[0].Reference.ShouldBe(1L);
            loadedEntries[0].ObjectId.ShouldBe(0L);
            
            loadedEntries[1].Key.ShouldBe("someKey2");
            loadedEntries[1].Value.ShouldBe("someValue");
            loadedEntries[1].ObjectId.ShouldBe(0L);
            
            fileStorage.Clear();
            fileStorage.Dispose();
        }
    }
}