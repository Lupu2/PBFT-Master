using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Cleipnir.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cleipnir.StorageEngine.SimpleFile
{
    public class SimpleFileStorageEngine : IStorageEngine
    {
        private readonly string _path;
        
        public SimpleFileStorageEngine(string path, bool deleteExisting = false)
        {
            _path = path;
            if (deleteExisting)
                Clear();
        } 

        public void Persist(DetectedChanges detectedChanges)
        {
            var newEntries = detectedChanges.NewEntries
                .Select(e =>
                    {
                        if (e.Reference.HasValue)
                            return (IEntry) new NewReference
                            {
                                EntryType = EntryType.NewReferenceEntry,
                                ObjectId = e.ObjectId,
                                Key = e.Key,
                                Reference = e.Reference.Value
                            };
                        if (e.Value == null)
                            return new NewNullValueEntry
                            {
                                EntryType = EntryType.NewNullValueEntry,
                                ObjectId = e.ObjectId,
                                Key = e.Key
                            };
                        else
                            return new NewValueEntry
                            {
                                EntryType = EntryType.NewValueEntry,
                                ObjectId = e.ObjectId,
                                Key = e.Key,
                                Value = new JRaw(e.Value.ToJson()),
                                ValueType = e.Value.GetType().SimpleQualifiedName()
                            };
                    }
                );
            IEnumerable<IEntry> removedEntries = detectedChanges.RemovedEntries
                .Select(e => new Removed
                {
                    EntryType = EntryType.RemovedEntry,
                    ObjectId = e.ObjectId,
                    Key = e.Key
                });
            IEnumerable<IEntry> garbageCollected = detectedChanges.GarbageCollectableIds
                .Select(gc => new GarbageCollected {EntryType = EntryType.GarbageCollectedEntry, ObjectId = gc});
            IEnumerable<IEntry> serializerTypeEntries = detectedChanges.NewSerializerTypes
                .Select(e => new SerializerType
                {
                    EntryType = EntryType.SerializerTypeEntry,
                    ObjectId = e.ObjectId,
                    Type = e.SerializerType.SimpleQualifiedName()
                });

            var allJson = newEntries
                .Concat(removedEntries)
                .Concat(garbageCollected)
                .Concat(serializerTypeEntries)
                .Select(JsonConvert.SerializeObject);

            File.AppendAllLines(_path, allJson);
        }

        public StoredState Load()
        {
            var lines = File.ReadAllLines(_path);

            var entries = lines
                .Select(json => new
                {
                    Json = json, 
                    Entry = JsonConvert.DeserializeObject<Entry>(json).EntryType
                })
                .Select(a => a.Entry switch
                {
                    EntryType.NewValueEntry => (IEntry) JsonConvert.DeserializeObject<NewValueEntry>(a.Json),
                    EntryType.NewNullValueEntry => JsonConvert.DeserializeObject<NewNullValueEntry>(a.Json),
                    EntryType.NewReferenceEntry => JsonConvert.DeserializeObject<NewReference>(a.Json),
                    EntryType.RemovedEntry => JsonConvert.DeserializeObject<Removed>(a.Json),
                    EntryType.GarbageCollectedEntry => JsonConvert.DeserializeObject<GarbageCollected>(a.Json),
                    EntryType.SerializerTypeEntry => JsonConvert.DeserializeObject<SerializerType>(a.Json),
                    _ => throw new ArgumentOutOfRangeException()
                })
                .ToList();
            
            var serializerTypeEntries = entries
                .OfType<SerializerType>()
                .ToDictionary(
                    st => st.ObjectId, 
                    st => Type.GetType(st.Type)
                );


            var storageEntries = entries
                .Where(e => e is not SerializerType)
                .Aggregate(
                    ImmutableDictionary<ObjectIdAndKey, StorageEntry>.Empty,
                    (d, e) => e switch
                    {
                        GarbageCollected gc => d
                            .Where(kv => gc.ObjectId != kv.Key.ObjectId)
                            .ToImmutableDictionary(
                                kv => kv.Key,
                                kv => kv.Value
                            ),
                        NewNullValueEntry ne =>
                            d.SetItem(
                                new ObjectIdAndKey(ne.ObjectId, ne.Key),
                                new StorageEntry(ne.ObjectId, ne.Key, null)
                            ),
                        NewReference ne => d.SetItem(
                            new ObjectIdAndKey(ne.ObjectId, ne.Key),
                            new StorageEntry(ne.ObjectId, ne.Key, ne.Reference)
                        ),
                        NewValueEntry ne => d.SetItem(
                            new ObjectIdAndKey(ne.ObjectId, ne.Key),
                            new StorageEntry(
                                ne.ObjectId,
                                ne.Key,
                                JsonConvert.DeserializeObject(ne.Value.ToString(), Type.GetType(ne.ValueType))
                            )
                        ),
                        Removed r => d.Remove(new ObjectIdAndKey(r.ObjectId, r.Key)),
                        _ => throw new ArgumentOutOfRangeException(nameof(e), e, "Unexpected entry type")
                    })
                .Values
                .GroupBy(se => se.ObjectId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            
            return new StoredState(serializerTypeEntries, storageEntries);
        }

        private record ObjectIdAndKey (long ObjectId, string Key);

        public void Clear() => File.Delete(_path);
        public bool Exist => File.Exists(_path);

        public void Dispose() { }

        private enum EntryType
        {
            NewValueEntry = 0,
            NewNullValueEntry = 1,
            NewReferenceEntry = 2,
            RemovedEntry = 3,
            GarbageCollectedEntry = 4,
            SerializerTypeEntry = 5,
        }

        private interface IEntry
        {
            public EntryType EntryType { get; }
        }

        private class Entry : IEntry
        {
            public EntryType EntryType { get; set; }
        }
        
        private class NewValueEntry : IEntry
        {
            public EntryType EntryType { get; set; }
            public long ObjectId { get; set; }
            public string Key { get; set; }
            public JRaw Value { get; set; }
            public string ValueType { get; set; }
        }

        private class NewNullValueEntry : IEntry
        {
            public EntryType EntryType { get; set; }
            public long ObjectId { get; set; }
            public string Key { get; set; }
        }

        private class NewReference : IEntry
        {
            public EntryType EntryType { get; set; }
            
            public long ObjectId { get; set; }
            public string Key { get; set; }
            public long Reference { get; set; }
        }
        
        private class Removed : IEntry
        {
            public EntryType EntryType { set; get; }
            public long ObjectId { get; set; }
            public string Key { get; set; }
        }

        private class GarbageCollected : IEntry
        {
            public EntryType EntryType { set; get; }
            public long ObjectId { get; set; }
        }

        private class SerializerType : IEntry
        {
            public EntryType EntryType { set; get; }
            public long ObjectId { get; set; }
            public string Type { get; set; }
        }
    }
}
