using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.StorageEngine.SimpleFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.FileStorageEngine
{
    [TestClass]
    public class ObjectStoreTests
    {
        [TestMethod]
        public void ObjectsGraphCanBeStoredAndLoadedAgain()
        {
            var storageEngine = new SimpleFileStorageEngine(nameof(ObjectStoreTests), true);
            var objectStore = new ObjectStore(storageEngine);

            var parent = new Person()
            {
                Name = "Father",
                Parent = null,
            };
            var child = new Person()
            {
                Name = "Child",
                Parent = parent
            };
            
            objectStore.Attach(child);
            objectStore.Persist();
            
            storageEngine.Dispose();

            storageEngine = new SimpleFileStorageEngine(nameof(ObjectStoreTests), false);

            objectStore = ObjectStore.Load(storageEngine);
            
            var loadedChild = objectStore.Resolve<Person>();
            loadedChild.Name.ShouldBe("Child");
            loadedChild.Parent.Name.ShouldBe("Father");
        }

        private class Person : IPersistable
        {
            public Person Parent { get; set; }
            public string Name { get; set; }
            
            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Parent), Parent);
                sd.Set(nameof(Name), Name);
            }

            private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new Person()
                {
                    Name = sd[nameof(Name)].ToString(),
                    Parent = (Person) sd[nameof(Parent)]
                };
            }
        }
    }
}