using System;
using System.Collections.Generic;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency.Deserialization
{
    public static class DeserializationExtensions
    {
        public static T Get<T>(this IReadOnlyDictionary<string, object> d, string key) => (T) d[key];

        public static T Get<T>(this IReadOnlyDictionary<string, object> d, string key, T fallback)
            => d.ContainsKey(key) ? (T) d[key] : fallback;

        public static void ResolveReference<T>(
            this IReadOnlyDictionary<string, object> d,
            string key,
            Action<T> onResolved) => ((Reference) d[key]).DoWhenResolved(onResolved);
    }
}
