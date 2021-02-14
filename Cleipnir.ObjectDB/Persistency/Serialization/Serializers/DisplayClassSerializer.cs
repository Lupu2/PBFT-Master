using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class DisplayClassSerializer : ISerializer
    {
        public long Id { get; }
        public object Instance { get; }

        public DisplayClassSerializer(long id, object instance)
        {
            Id = id;
            Instance = instance;
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set("Type", Instance.GetType().SimpleQualifiedName());
            Instance.SerializeReferencesUsingReflectionInto(sd, helper);
        }

        public static DisplayClassSerializer Deserialize(long id, IReadOnlyDictionary<string, object> stateMap, ISet<object> ephemeralInstances)
        {
            var @type = (string) stateMap["Type"];
            var instance = Activator.CreateInstance(Type.GetType(@type));

            stateMap.DeserializeReferencesUsingReflectionInto(instance, ephemeralInstances, "Type");

            return new DisplayClassSerializer(id, instance);
        }
    }

    public static class DisplayClassExtensions
    {
        public static bool IsDisplayClass(this Type t)
            => t.Name.Contains("<");

        public static IEnumerable<object> FindDisplayClasses(object root)
        {
            IEnumerable<object> FindChildDisplayClasses(object instance)
            {
                var displayChildren = instance
                    .GetType()
                    .GetFields()
                    .Select(f => new { FieldInfo = f, Value = f.GetValue(root) })
                    .Where(a => a.Value != null)
                    .Where(a => a.Value.GetType().IsDisplayClass());

                var toReturn = displayChildren.Select(a => a.Value).SelectMany(FindChildDisplayClasses);
                if (instance.GetType().IsDisplayClass())
                    toReturn = toReturn.Append(instance);

                return toReturn;
            }

            return FindChildDisplayClasses(root);
        }
    }
}
