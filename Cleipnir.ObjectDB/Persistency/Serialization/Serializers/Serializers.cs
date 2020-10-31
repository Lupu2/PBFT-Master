using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Helpers.DataStructures;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class Serializers : IEnumerable<ISerializer>
    {
        private readonly ISerializerFactory _serializerFactory;
        private readonly DictionaryWithDefault<object, long> _objectToId;

        private readonly IDictionary<long, ISerializer> _serializers = new Dictionary<long, ISerializer>();

        private long _nextObjectId;

        public Serializers(long nextObjectId, ISerializerFactory serializerFactory)
        {
            _nextObjectId = nextObjectId;
            _serializerFactory = serializerFactory;
            _objectToId = new DictionaryWithDefault<object, long>(
                _ => _nextObjectId++,
                new Dictionary<object, long>(new ObjectReferenceEqualityComparer<object>())
            );
        }

        public ISerializer this[long id] => _serializers[id];

        public bool IsSerializable(object o) => _serializerFactory.IsSerializable(o);

        public ISerializer AddAndWrapUp(object o)
        {
            var id = _objectToId[o];
            if (_serializers.ContainsKey(id))
                return _serializers[id];

            var serializer = _serializerFactory.CreateSerializer(o, id);

            _serializers[id] = serializer;

            return serializer;
        }

        public void Add(ISerializer serializer)
        {
            _objectToId[serializer.Instance] = serializer.Id;
            _serializers[serializer.Id] = serializer;
        }

        public void Remove(long id)
        {
            if (!_serializers.ContainsKey(id))
                return;

            var serializer = _serializers[id];
            _serializers.Remove(id);
            _objectToId.Remove(serializer.Instance);
        }

        public Tuple<ISerializer, long> GetSerializerOrNextObjectIndex<T>(T obj)
        {
            return !_objectToId.ContainsKey(obj) 
                ? Tuple.Create(default(ISerializer), _nextObjectId++) 
                : Tuple.Create(_serializers[_objectToId[obj]], -1L);
        } 

        public bool ContainsKey(long id) => _serializers.ContainsKey(id);

        public IEnumerable<ISerializer> GetAll() => _serializers.Values.ToList();

        public IEnumerator<ISerializer> GetEnumerator() => GetAll().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class ObjectReferenceEqualityComparer<T> : EqualityComparer<T> where T : class
        {
            public override bool Equals(T x, T y) => ReferenceEquals(x, y);

            public override int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
