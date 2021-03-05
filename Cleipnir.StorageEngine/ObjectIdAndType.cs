using System;

namespace Cleipnir.StorageEngine
{
    public record ObjectIdAndType(long ObjectId, Type SerializerType) { }
}