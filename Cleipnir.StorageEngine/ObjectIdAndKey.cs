using System;

namespace Cleipnir.StorageEngine
{
    public readonly struct ObjectIdAndKey
    {
        public ObjectIdAndKey(long objectId, string key)
        {
            ObjectId = objectId;
            Key = key;
        }

        public long ObjectId { get; }
        public string Key { get; }

        public void Deconstruct(out long objectId, out string key)
        {
            objectId = ObjectId;
            key = Key;
        }

        public override bool Equals(object obj)
            => obj is ObjectIdAndKey other &&
               ObjectId == other.ObjectId &&
               Key == other.Key;

        public override int GetHashCode() => HashCode.Combine(ObjectId, Key);
    }
}