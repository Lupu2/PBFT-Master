using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cleipnir.ObjectDB.Persistency.Deserialization
{
    public class DeserializationMethodInvoker
    {
        internal static object Invoke(long id, Type type, IReadOnlyDictionary<string, object> dict, ISet<object> ephemeralInstances)
        {
            var deSerializeMethod = type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (deSerializeMethod == null) //try with constructor instead
            {
                var constructor = type
                    .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(c => c.GetParameters().Any(p => p.ParameterType == typeof(IReadOnlyDictionary<string, object>)));

                return constructor.Invoke(new object[] { dict });
            }

            var resolveds = new List<object>();

            foreach (var p in deSerializeMethod.GetParameters())
            {
                if (p.ParameterType == typeof(long))
                    resolveds.Add(id);
                else if (p.ParameterType == typeof(IReadOnlyDictionary<string, object>))
                    resolveds.Add(dict);
                else if (p.ParameterType == typeof(ISet<object>))
                    resolveds.Add(ephemeralInstances);
                else
                    resolveds.Add(ephemeralInstances.Single(i => p.ParameterType.IsInstanceOfType(i)));
            }

            var deserialized = deSerializeMethod.Invoke(null, resolveds.ToArray());

            return deserialized;
        }
    }
}
