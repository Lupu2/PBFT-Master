using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB
{
    public class ObjectStore
    {
        private readonly Persister _persister;

        public RootsInstance Roots { get; }
        public IStorageEngine StorageEngine { get; }

        //Used when data does not already exists
        public ObjectStore(IStorageEngine storageEngine)
        {
            StorageEngine = storageEngine;
            var roots = new RootsInstance();
            var serializers = new Serializers(0, new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            
            var serializersTypeEntries = new SerializersTypeEntries();

            serializers.Add(new PersistableSerializer(RootsInstance.PersistableId, roots));

            Roots = roots;

            _persister = new Persister(StorageEngine, Roots, serializers, stateMaps, serializersTypeEntries);
        }

        //Used when data exists
        internal ObjectStore(
            RootsInstance roots, 
            StateMaps stateMaps, Serializers serializers, SerializersTypeEntries serializersTypeEntries,
            IStorageEngine storageEngine)
        {
            Roots = roots;
            StorageEngine = storageEngine;

            _persister = new Persister(StorageEngine, Roots, serializers, stateMaps, serializersTypeEntries);
        }

        public void Attach(object toAttach) => Roots.Entangle(toAttach);

        public T Resolve<T>() => Roots.Resolve<T>();
        public IEnumerable<T> ResolveAll<T>() => Roots.ResolveAll<T>();

        public void Persist() => _persister.Serialize();

        public static ObjectStore Load(IStorageEngine storageEngine, params object[] ephemeralInstances) 
            => Deserializer.Load(storageEngine, new HashSet<object>(ephemeralInstances), new SerializerFactory());
    }
}
