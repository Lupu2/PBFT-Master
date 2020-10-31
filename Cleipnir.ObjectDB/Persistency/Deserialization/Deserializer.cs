using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Helpers.DataStructures;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Deserialization
{
    internal static class Deserializer
    {
        public static ObjectStore Load(IStorageEngine storageEngine, ISet<object> ephemeralInstances, ISerializerFactory serializerFactory)
        {
            var valuesDictionaries = new DictionaryWithDefault<long, Dictionary<string, object>>(_ => new Dictionary<string, object>());

            //Load all entries from log file
            var entries = storageEngine.Load().ToList();

            var serializers = new Serializers(entries.Select(e => e.ObjectId).Max() + 1, serializerFactory);
            var stateMaps = new StateMaps(serializers);

            var entriesPerOwner = entries
                .GroupBy(keySelector: a => a.ObjectId, elementSelector: a => a)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(a => a.Key, a => a)
                );

            var serializersTypeEntries = new SerializersTypeEntries(entriesPerOwner[SerializersTypeEntries.PersistableId].Values);

            var toDeserialize = new Queue<long>();
            var referenced = new DictionaryWithDefault<long, List<Reference>>(_ => new List<Reference>());

            var deserializationHelper = new DeserializationHelper();
            ephemeralInstances.Add(deserializationHelper);

            ISerializer Deserialize(long id, ImmutableList<long> path)
            {
                if (path.Contains(id))
                {
                    var pathStr = path.Aggregate(
                        seed: new { Found = false, Elms = Enumerable.Empty<long>() },
                        func: (akk, pId) =>
                            akk.Found || pId == id ? new { Found = true, Elms = akk.Elms.Append(pId) } : akk
                    ).Elms
                        .Select(pId => valuesDictionaries[pId].ContainsKey("Type") ? Type.GetType(valuesDictionaries[pId]["Type"].ToString()).Name : "?")
                        .StringJoin("-> ");

                    throw new Exception("Circular dependency detected: " + pathStr);
                }

                path = path.Add(id);

                if (serializers.ContainsKey(id))
                    return serializers[id];

                var ownerEntries = entriesPerOwner.ContainsKey(id)
                    ? entriesPerOwner[id]
                    : new Dictionary<string, StorageEntry>();

                var resolvedValues = valuesDictionaries[id];

                //deserialize non-referencing values
                var resolvableEntries = ownerEntries
                    .Values
                    .Where(e => !e.Reference.HasValue)
                    .ToList();

                foreach (var resolvableEntry in resolvableEntries)
                    resolvedValues[resolvableEntry.Key] = resolvableEntry.Value;

                var referencedEntries = ownerEntries.Where(e => e.Value.Reference.HasValue);

                foreach (var referencedEntry in referencedEntries)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    var wp = Deserialize(referencedEntry.Value.Reference.Value, path);
                    resolvedValues[referencedEntry.Key] = wp.Instance;
                }

                var serializer = (ISerializer) DeserializationMethodInvoker
                    .Invoke(
                        id,
                        serializersTypeEntries[id],
                        valuesDictionaries[id],
                        ephemeralInstances
                    );

                stateMaps[id] = new StateMap(serializers, resolvedValues);
                serializers.Add(serializer);

                if (serializer.Instance is Reference r && r.Id.HasValue)
                {
                    toDeserialize.Enqueue(r.Id.Value);
                    referenced[r.Id.Value].Add(r);
                }

                return serializer;
            }

            var roots = (RootsInstance) Deserialize(RootsInstance.PersistableId, ImmutableList<long>.Empty).Instance;
            ephemeralInstances.Add(roots);

            while (toDeserialize.Any())
                Deserialize(toDeserialize.Dequeue(), ImmutableList<long>.Empty);

            foreach (var serializer in serializers)
                foreach (var reference in referenced[serializer.Id])
                    reference.SetSerializer(serializer);

            deserializationHelper.ExecutePostInstanceCreationCallbacks();

            return new ObjectStore(roots, stateMaps, serializers, serializersTypeEntries, storageEngine);
        }
    }
}
