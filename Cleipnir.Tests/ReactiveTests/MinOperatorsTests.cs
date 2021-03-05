using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class MinOperatorsTests_
    {
        [TestMethod]
        public void MinIntOperatorTest()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);

            var source = new Source<int>();
            var valueHolder = new ValueHolder<int>(); //litterly just persist object that has a single object, in this case int value
            source.Min().CallOnEvent(valueHolder.SetValue);
            
            source.Emit(3);
            
            valueHolder.Value.ShouldBe(3);
            
            source.Emit(5);
            
            valueHolder.Value.ShouldBe(3);
            
            source.Emit(1);
            
            source.Emit(-1);
            
            valueHolder.Value.ShouldBe(-1);
            
            os.Attach(source);
            os.Attach(valueHolder);
            os.Persist();
            
            os = ObjectStore.Load(storage);
            source = os.Resolve<Source<int>>();
            valueHolder = os.Resolve<ValueHolder<int>>();
            valueHolder.Value.ShouldBe(-1);
            source.Emit(-2);
            valueHolder.Value.ShouldBe(-2);
        }
        
        [TestMethod]
        public void MinDateTimeOperatorTest()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);

            var source = new Source<DateTime>();
            var valueHolder = new ValueHolder<DateTime>();
            source.Min().CallOnEvent(valueHolder.SetValue);
            
            var firsttimeobj = new DateTime(12, 11, 10);
            source.Emit(firsttimeobj);
            valueHolder.Value.ShouldBe(firsttimeobj);

            var secondtimeobj = new DateTime(10, 9, 8);
            source.Emit(secondtimeobj);
            valueHolder.Value.ShouldBe(secondtimeobj);

            var thirdtimeobj = new DateTime(10, 11, 12);
            source.Emit(thirdtimeobj);
            valueHolder.Value.ShouldBe(secondtimeobj);
            
            os.Attach(source);
            os.Attach(valueHolder);
            os.Persist();
            
            os = ObjectStore.Load(storage);
            source = os.Resolve<Source<DateTime>>();
            valueHolder = os.Resolve<ValueHolder<DateTime>>();
            valueHolder.Value.ShouldBe(secondtimeobj);

            var fourthtimeobj = new DateTime(5, 5, 5);
            source.Emit(fourthtimeobj);
            valueHolder.Value.ShouldBe(fourthtimeobj);
        }
    }
}