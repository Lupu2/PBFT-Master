using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Rx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class DistinctByOperatorTests
    {
        [TestMethod]
        public void DuplicatesAreNotEmitted()
        {
            var source = new Source<Person>();
            Person latestResult = null;
            var subscription = new object();
            source.DistinctBy(p => p.Name).Subscribe(subscription, p => latestResult = p);
            
            latestResult.ShouldBeNull();
            source.Emit(new Person() {Name = "Peter"});
            latestResult.Name.ShouldBe("Peter");
            source.Emit(new Person() {Name = "Ole"});
            latestResult.Name.ShouldBe("Ole");
            source.Emit(new Person() {Name = "Peter"});
            latestResult.Name.ShouldBe("Ole");
        }

        private class Person : IPropertyPersistable
        {
            public string Name { get; set; }
        }
    }
}