using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Helpers
{
    public static class SerializationHelpers
    {
        // ** LIST SERIALIZER ** //
        public static void SerializeInto<T>(this IEnumerable<T> ts, StateMap sd, string keyPrefix = "Elms")
        {
            ts = ts.ToList();

            var existingListSize = sd.ContainsKey($"{keyPrefix}.List.Size") ? (int) sd.Get($"{keyPrefix}.List.Size") : -1;

            //remove existing entries that are not be overwritten
            for (var j = ts.Count(); j < existingListSize; j++)
                sd.Remove($"{keyPrefix}.List.{j}");

            var i = 0;
            
            sd.Set($"{keyPrefix}.List.Size", ts.Count());
            
            foreach (var t in ts)
            {
                sd.Set($"{keyPrefix}.List.{i}", t);
                i++;
            }
        }

        public static List<T> DeserializeIntoList<T>(this IReadOnlyDictionary<string, object> sd, string keyPrefix = "Elms")
        {
            var size = (int) sd[$"{keyPrefix}.List.Size"];
            
            var list = new List<T>();
            for (var i = 0; i < size; i++)
                list.Add((T) sd[$"{keyPrefix}.List." + i]);

            return list;
        }
    }
}
