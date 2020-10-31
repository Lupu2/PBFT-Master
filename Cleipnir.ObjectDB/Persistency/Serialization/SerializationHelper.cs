using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency.Serialization
{
    public class SerializationHelper
    {
        private readonly Serializers.Serializers _serializers;

        internal SerializationHelper(Serializers.Serializers wps) => _serializers = wps;

        public Reference GetReference(object o) 
        {
            return o != null && _serializers.IsSerializable(o) 
                ? new Reference(_serializers.AddAndWrapUp(o)) 
                : new Reference(o);
        } 
    }
}
