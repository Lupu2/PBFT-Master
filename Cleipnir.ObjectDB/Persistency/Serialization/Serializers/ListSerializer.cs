using System.Collections.Generic;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class ListSerializer
    {
        public static ListSerializer<T> Create<T>(long id, List<T> list) => new ListSerializer<T>(id, list);
    }
    internal class ListSerializer<T> : ISerializer
    {
        public ListSerializer(long id, List<T> list)
        {
            Id = id;
            List = list;
            _prevCount = list.Count;
        }

        public long Id { get; }
        public object Instance => List;
        private List<T> List { get; }
        private int _prevCount;

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            //remove existing entries that are not be overwritten
            for (var i = List.Count; i < _prevCount; i++)
                sd.Remove(i.ToString());
            sd.Set("Size", List.Count);
            for (var i = 0; i < List.Count; i++)
                sd.Set(i.ToString(), List[i]);

            _prevCount = List.Count;
        }

        private static ListSerializer<T> Deserialize(long id, IReadOnlyDictionary<string, object> sd)
        {
            var size = (int)sd["Size"];

            var list = new List<T>();
            for (var i = 0; i < size; i++)
                list.Add((T)sd[i.ToString()]);

            return new ListSerializer<T>(id, list);
        }
    }
}
