using System;

namespace Cleipnir.StorageEngine
{
    public sealed class StorageEntry
    {
        public long ObjectId { get; }
        public string Key { get; }

        public bool IsReference => Reference != null;
        public long? Reference { get; }
        public object Value { get; }

        public StorageEntry(long owner, string key, long reference)
        {
            Reference = reference;
            ObjectId = owner;
            Key = key;
        }

        public StorageEntry(long owner, string key, object value)
        {
            Value = value;
            ObjectId = owner;
            Key = key;
        }

        public StorageEntry(long objectId, string key, object value, long? reference)
        {
            ObjectId = objectId;
            Key = key;
            Value = value;
            Reference = reference;
        }

        public override bool Equals(object obj) 
            => ReferenceEquals(this, obj) || 
               obj is StorageEntry other && 
               ObjectId == other.ObjectId && 
               Key == other.Key && 
               Reference == other.Reference && 
               Equals(Value, other.Value);

        public override int GetHashCode() => HashCode.Combine(ObjectId, Key, Reference, Value);
    }
}