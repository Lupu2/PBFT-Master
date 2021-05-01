using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Helpers
{
    public static class ReflectiveSerializer
    {
        public static void SerializeReferencesUsingReflectionInto(this object instance, StateMap sd, SerializationHelper helper)
        {
            var instanceType = instance.GetType();
            var fields = instanceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var fieldValues = fields
                .Select(f => new { Field = f, Value = f.GetValue(instance) })
                .Where(a => a.Value == null ||
                            a.Field.FieldType.IsPrimitive ||
                            a.Field.FieldType == typeof(DateTime) ||
                            a.Field.FieldType == typeof(string) ||
                            a.Value is IPersistable ||
                            a.Value is IPropertyPersistable ||
                            a.Value.GetType().IsDisplayClass() ||
                            a.Value is Delegate
                )
                .Select(a => new FieldNameAndValue(a.Field.Name, a.Value));

            foreach (var fieldAndValue in fieldValues)
                sd.Set(fieldAndValue.Name, helper.GetReference(fieldAndValue.Value));
        }

        public static void DeserializeReferencesUsingReflectionInto(this IReadOnlyDictionary<string, object> dict, object instance, ISet<object> ephemeralInstances, params string[] ignoreKeys)
        {
            var values = dict.Where(kv => !ignoreKeys.Contains(kv.Key));

            var instanceType = instance.GetType();

            foreach (var (key, value) in values)
            {
                var field = instanceType.GetField(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var reference = (Reference) value;
                reference.DoWhenResolved(v =>
                {
                    field.SetValue(instance, v);
                });
            }
        }

        internal class FieldNameAndValue
        {
            public FieldNameAndValue(string name, object value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public object Value { get; }

            public override string ToString()
            {
                return Name + ": " + Value;
            }
        }

    }
}
