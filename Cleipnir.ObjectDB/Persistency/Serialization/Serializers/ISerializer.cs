namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    public interface ISerializer
    {
        long Id { get; }
        object Instance { get; }
        void Serialize(StateMap stateToSerialize, SerializationHelper helper);
    }
}
