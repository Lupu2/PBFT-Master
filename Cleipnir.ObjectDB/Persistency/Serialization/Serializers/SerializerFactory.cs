using System;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    public class SerializerFactory 
    {
        public ISerializer CreateSerializer(object o, long id)
        {
            switch (o)
            {
                case IPersistable p:
                    return new PersistableSerializer(id, p);
                case Reference r: 
                    return new ReferenceSerializer(id, r);
                case Exception e:
                    return new ExceptionSerializer(id, e);
                case IPropertyPersistable pp:
                    return new PropertySerializer(id, pp);
            }

            if (o is Delegate d)
            {
                var type = o.GetType();
                
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Action<>))
                    return new DelegateSerializer(id, d);

                var genericTypeArgument = o.GetType().GetGenericArguments()[0];

                var method = typeof(ActionSerializer).GetMethod(nameof(ActionSerializer.Create));
                var generic = method.MakeGenericMethod(genericTypeArgument);
                var result = generic.Invoke(null, new[] { id, o });

                return (ISerializer)result;
            }

            if (o.GetType().IsDisplayClass())
                return new DisplayClassSerializer(id, o);

            throw new NotImplementedException();
        }

        public bool IsSerializable(object o)
            => o is IPersistable || o is Reference || o is Exception || o.GetType().IsDisplayClass() || o is Delegate || o is StateMap || o is IPropertyPersistable;
    }
}
