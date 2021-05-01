using Cleipnir.ObjectDB.PersistentDataStructures;
using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    public class AllOperatorTests
    {
        public bool AllHigher<T>(int n, CList<int> list)
        {
            foreach (var li in list)
            {
                if (li < n) return false;
            }

            return true;
        }
        
        [TestMethod]
        public void AllOperatorTest()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);
            
            var source = new Source<bool>();
            throw new NotImplementedException();

        }
    }
}