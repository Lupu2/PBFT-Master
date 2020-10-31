namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    public interface IPersistable 
    {
        void Serialize(StateMap stateToSerialize, SerializationHelper helper);
    }
}
