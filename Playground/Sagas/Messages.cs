using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Playground.Sagas
{
    public class StartCommand : IPropertyPersistable
    {
        public string InstanceId { get; set; }
        public string GroupId { get; set; }
        public string CommandMsg { get; set; }
    }

    public class FirstCommand : IPropertyPersistable
    {
        public string CommandMsg { get; set; }
    }

    public class FirstReply : IPropertyPersistable
    {
        public string InstanceId { get; set; }
        public string GroupId { get; set; }
        public string ReplyMessage { get; set; }
    }

    public class SecondCommand : IPropertyPersistable
    {
        public string CommandMsg { get; set; }
    }

    public class SecondReply : IPropertyPersistable
    {
        public string InstanceId { get; set; }
        public string GroupId { get; set; }
        public string ReplyMessage { get; set; }
    }
}
