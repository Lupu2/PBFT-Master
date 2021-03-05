using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.CircularDependencyDetector
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void DetectCircularDependencyBetween4People()
        {
            var detector = new ObjectDB.Persistency.CircularDependencyDetector();

            var peter = new Person { Name = "Peter" };
            var ole = new Person { Name = "Ole", Other = peter };
            var pia = new Person { Name = "Pia", Other = ole };
            var hanne = new Person { Name = "Hanne", Other = pia };
            
            peter.Other = pia;

            var peterS = new PersistableSerializer(0, peter);
            var oleS = new PersistableSerializer(1, ole);
            var piaS = new PersistableSerializer(2, pia);
            var hanneS = new PersistableSerializer(3, hanne);

            var serializers = new Serializers(new SerializerFactory()) { peterS, oleS, piaS, hanneS };

            var stateMaps = new StateMaps(serializers)
            {
                [0] = new(serializers),
                [1] = new(serializers),
                [2] = new(serializers),
                [3] = new(serializers)
            };

            peter.Serialize(stateMaps[0], null);
            ole.Serialize(stateMaps[1], null);
            pia.Serialize(stateMaps[2], null);
            hanne.Serialize(stateMaps[3], null);

            var circularChain = detector.Check(hanneS, stateMaps).OfType<Person>().ToList();
            circularChain.Count.ShouldBe(4);
            circularChain[0].Name.ShouldBe("Pia");
            circularChain[1].Name.ShouldBe("Ole");
            circularChain[2].Name.ShouldBe("Peter");
            circularChain[3].Name.ShouldBe("Pia");
        }
        
        [TestMethod]
        public void DetectNonCircularDependencyBetween4People()
        {
            var detector = new ObjectDB.Persistency.CircularDependencyDetector();

            var peter = new Person { Name = "Peter" };
            var ole = new Person {Name = "Ole", Other = peter};
            var pia = new Person {Name = "Pia", Other = ole};
            var hanne = new Person {Name = "Hanne", Other = pia};
            
            var peterS = new PersistableSerializer(0, peter);
            var oleS = new PersistableSerializer(1, ole);
            var piaS = new PersistableSerializer(2, pia);
            var hanneS = new PersistableSerializer(3, hanne);

            var serializers = new Serializers(new SerializerFactory()) { peterS, oleS, piaS, hanneS };

            var stateMaps = new StateMaps(serializers)
            {
                [0] = new(serializers),
                [1] = new(serializers),
                [2] = new(serializers),
                [3] = new(serializers)
            };

            peter.Serialize(stateMaps[0], null);
            ole.Serialize(stateMaps[1], null);
            pia.Serialize(stateMaps[2], null);
            hanne.Serialize(stateMaps[3], null);

            var circularChain = detector.Check(hanneS, stateMaps).OfType<Person>().ToList();
            circularChain.Count.ShouldBe(0);
        }
    }
}