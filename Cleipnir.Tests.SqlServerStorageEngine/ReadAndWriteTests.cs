using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.SqlServerStorageEngine
{
    [TestClass]
    public class ReadAndWriteTests
    {
        [TestMethod]
        public void ReadWriteTest()
        {
            var testHelper = new TestHelper();

            var os = new ObjectStore(testHelper.StorageEngineEngine);

            var p = new P();
            os.Attach(p);
            p.SetValue("HELLO WORLD");
            os.Persist();

            using var conn = testHelper.CreateConnection(); 
            conn.QuerySingle<int>(@"
                    SELECT COUNT(*)
                    FROM [CleipnirTests].[dbo].[KeyValues]
                    WHERE [Value] = '""HELLO WORLD""'"
            ).ShouldBe(1);

            p.RemoveValue();
            os.Persist();

            conn.QuerySingle<int>(@"
                    SELECT COUNT(*)
                    FROM [CleipnirTests].[dbo].[KeyValues]
                    WHERE [Value] = '""HELLO WORLD""'"
            ).ShouldBe(0);
        }

        private class P : IPersistable
        {
            private string _value;

            public void SetValue(string value) => _value = value;

            public void RemoveValue() => _value = null;

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_value == null)
                    sd.Remove(nameof(_value));
                else
                    sd.Set(nameof(_value), _value);
            }

            private static P Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return sd.ContainsKey(nameof(_value))
                    ? new P() { _value = sd.Get<string>(nameof(_value)) }
                    : new P();
            }
        }
    }
}
