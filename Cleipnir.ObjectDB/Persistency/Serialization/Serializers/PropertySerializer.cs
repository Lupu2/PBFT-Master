using System;
using System.Collections.Generic;
using System.Reflection;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
	internal class PropertySerializer : ISerializer
	{
        public PropertySerializer(long id, IPropertyPersistable p)
        {
            Id = id;
            PropertyPersistable = p;     
            if (p.GetType().GetConstructor(Type.EmptyTypes) == null) 
                throw new ArgumentException();
        }

        public long Id { get; }
        public object Instance => PropertyPersistable;
        private IPropertyPersistable PropertyPersistable { get; }        

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            foreach(var propInfo in PropertyPersistable.GetType().GetProperties())
                sd.Set(propInfo.Name, propInfo.GetValue(PropertyPersistable));

            sd.Set("Type", PropertyPersistable.GetType().SimpleQualifiedName());
        }

        private static PropertySerializer Deserialize(long id, IReadOnlyDictionary<string, object> sd)
        {
            var type = Type.GetType(sd["Type"].ToString());
            var obj = (IPropertyPersistable) Activator.CreateInstance(type);
            foreach(string key in sd.Keys)
			{
                if (key == "Type")
                    continue;
                type.GetProperty(key).SetValue(obj, sd[key]);
			}
            return new PropertySerializer(id, obj);
        }
    }
}
