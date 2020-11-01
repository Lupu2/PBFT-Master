﻿using System.Collections.Generic;
using System.Linq;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization
{
    internal class Persister
    {
        private readonly IStorageEngine _storageEngine;
        private readonly RootsInstance _roots;
        private readonly StateMaps _stateMaps;
        private readonly Serializers.Serializers _serializers;

        private readonly SerializersTypeEntries _serializersTypeEntries;

        public Persister(
            IStorageEngine storageEngine,
            RootsInstance roots,
            Serializers.Serializers serializers, 
            StateMaps stateMaps,
            SerializersTypeEntries serializersTypeEntries)
        {
            _storageEngine = storageEngine;
            _roots = roots;
            _serializers = serializers;
            _stateMaps = stateMaps;
            _serializersTypeEntries = serializersTypeEntries;
        }

        public void Serialize()
        {
            var detectedChanges = DetectChanges();
            if (detectedChanges.GarbageCollectables.Empty() &&
                detectedChanges.RemovedEntries.Empty() &&
                detectedChanges.StorageEntries.Empty())
                return;
            
            _storageEngine.Persist(detectedChanges);
        }

        private DetectedChanges DetectChanges()
        {
            var persistableChanges = new List<StorageEntry>();
            var removedEntries = new List<ObjectIdAndKey>();

            var readyToBeSerializeds = new Queue<ISerializer>(new[] { _serializers.AddAndWrapUp(_roots) });

            var serializationHelper = new SerializationHelper(_serializers);

            var serializedIds = new HashSet<long>();

            while (readyToBeSerializeds.Any())
            {
                var serializer = readyToBeSerializeds.Dequeue();
                var objectId = serializer.Id;
                if (serializedIds.Contains(objectId))
                    continue; //the object has already been serialized

                if (!_serializersTypeEntries.ContainsObjectId(objectId))
                    _serializersTypeEntries[objectId] = serializer.GetType();

                var stateMap = _stateMaps.Get(objectId);

                serializer.Serialize(stateMap, serializationHelper);

                if (serializer.Instance is Reference r && r.Id.HasValue)
                    readyToBeSerializeds.Enqueue(_serializers[r.Id.Value]);

                //add referenced serializers to serialization queue
                var referencedSerializers = stateMap.GetReferencedSerializers();

                foreach (var referencedSerializer in referencedSerializers)
                    readyToBeSerializeds.Enqueue(referencedSerializer);

                //pull and add statemap changes to global list of changes
                var changes = stateMap
                    .PullChangedEntries()
                    .Select(change =>
                        change.HoldsSerializer
                            ? new StorageEntry(objectId, change.Key, change.Serializer.Id)
                            : new StorageEntry(objectId, change.Key, change.Value)
                    );

                removedEntries.AddRange(stateMap.PullRemovedKeys().Select(key => new ObjectIdAndKey(objectId, key)));

                persistableChanges.AddRange(changes);

                //add just serialized entity to list of serialized ids in order to short circuit if/when reaching then same entity again
                serializedIds.Add(serializer.Id);
            }

            var serializersTypeEntriesChanges = _serializersTypeEntries.PullChanges();
            var removedSerializersTypeEntries = _serializersTypeEntries.PullRemoved();
            removedEntries.AddRange(removedSerializersTypeEntries);

            var allChanges = persistableChanges.Concat(serializersTypeEntriesChanges).ToList();

            var garbageCollectables = _stateMaps.Ids.Where(id => !serializedIds.Contains(id)).ToList();
            RemoveGarbageCollectables(garbageCollectables);

            return new DetectedChanges(allChanges, removedEntries, garbageCollectables);
        }

        private void RemoveGarbageCollectables(IEnumerable<long> garbageCollectables)
        {
            foreach (var garbageCollectable in garbageCollectables)
            {
                _serializers.Remove(garbageCollectable);
                _stateMaps.Remove(garbageCollectable);
                _serializersTypeEntries.Remove(garbageCollectable);
            }
        }
    }
}
