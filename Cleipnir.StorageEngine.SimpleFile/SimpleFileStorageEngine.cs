using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Cleipnir.StorageEngine.SimpleFile
{
    public class SimpleFileStorageEngine : IStorageEngine
    {
        private readonly string _path;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore};

        public SimpleFileStorageEngine(string path, bool deleteExisting = false)
        {
            _path = path;
            if (deleteExisting)
                Clear();
        } 

        public void Persist(DetectedChanges detectedChanges)
        {
            //todo add support for removed entries and garbage collected
            var csvEntries = detectedChanges.StorageEntries
                .Select(e =>
                    new Entry
                    {
                        ObjectId = e.ObjectId,
                        Key = e.Key,
                        Reference = e.Reference,
                        Value = e.Value == null
                            ? null
                            : e.Value is string s
                                ? s
                                : JsonConvert.SerializeObject(e.Value),
                        ValueType = e.Value?.GetType().FullName

                    })
                .Concat(detectedChanges.RemovedEntries.Select(r =>
                    new Entry
                    {
                        Key = r.Key,
                        ObjectId = r.ObjectId,
                        Removed = true
                    }))
                .Select(e => JsonConvert.SerializeObject(e, Formatting.None, SerializerSettings));
                

            File.AppendAllLines(_path, csvEntries);
        }

        public IEnumerable<StorageEntry> Load()
        {
            var lines = File.ReadAllLines(_path);

            //var dict = new Dictionary<Tuple<long, string>, StorageEntry>();

            var entries = lines
                .Select(JsonConvert.DeserializeObject<Entry>)
                .Aggregate(
                    ImmutableDictionary<Tuple<long, string>, Entry>.Empty,
                    (d, e) => 
                            e.Removed ? 
                                d.Remove(Tuple.Create(e.ObjectId, e.Key)) : 
                                d.SetItem(Tuple.Create(e.ObjectId, e.Key), e
                        ) 
                )
                .Values
                .Select(e => new StorageEntry(
                    e.ObjectId,
                    e.Key,
                    e.Value == null
                        ? null
                        : Type.GetType(e.ValueType) == typeof(string) ? e.Value : JsonConvert.DeserializeObject(e.Value, Type.GetType(e.ValueType), SerializerSettings),
                    e.Reference)
                );

            /*foreach (var entry in entries)
            {
                dict[Tuple.Create(entry.ObjectId, entry.Key)] = entry;
            }*/

            return entries;
        }

        public void Clear() => File.Delete(_path);
        public bool Exist => File.Exists(_path);

        public void Dispose() { } 

        private class Entry
        {
            public long ObjectId { get; set; }
            public string Key { get; set; }
            public bool Removed { get; set; }
            public string Value { get; set; }
            public string ValueType { get; set; }
            public long? Reference { get; set; }
        }
    }
}
