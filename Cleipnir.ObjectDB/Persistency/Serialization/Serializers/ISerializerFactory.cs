namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    public interface ISerializerFactory
    {
        ISerializer CreateSerializer(object o, long id);
        bool IsSerializable(object o);
    }
}