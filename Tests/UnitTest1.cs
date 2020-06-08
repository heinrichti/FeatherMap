using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using FeatherMap;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CustomMapper()
        {
            var mappingA = Mapping<TestA, TestA>.Auto(cfg => cfg.Direction(Direction.OneWay));
            var mappingC = Mapping<TestC, TestC>.New()
                .Bind(x => x.MyProperty, x => x.MyProperty, cfg => cfg.UseMapping(mappingA)).Build();

            var a = new TestC { MyProperty = new TestA { MyPropertyA = 1, MyPropertyB = 2, MyPropertyC = 3} };
            var b = new TestC();

            mappingC.MapToTarget(a, b);

            Assert.AreEqual(1, b.MyProperty.MyPropertyA);
            Assert.AreEqual(2, b.MyProperty.MyPropertyB);
            Assert.AreEqual(3, b.MyProperty.MyPropertyC);
        }

        [TestMethod]
        public void ConverterNotProvided()
        {
            Assert.ThrowsException<ArgumentException>(() => Mapping<TestA, TestB>.New()
                .Bind(x => x.MyPropertyA, x => x.MyPropertyD));
        }

        [TestMethod]
        public void Converter()
        {
            var converter = new TestConverter();

            var a = new TestB() { MyPropertyA = 1, MyPropertyD = "hello" };
            var b = new TestB();
            var mapping = Mapping<TestB, TestB>.New()
                .Bind(x => x.MyPropertyA, x => x.MyPropertyD, cfg => cfg.UseConverter(converter))
                .Build();

            mapping.MapToTarget(a, b);

            Assert.AreEqual("1", b.MyPropertyD);
            
            b.MyPropertyD = "25";
            mapping.MapToSource(a, b);

            Assert.AreEqual(25, a.MyPropertyA);
        }

        [TestMethod]
        public void AutoWithOverriddenMembers()
        {
            var mapping = Mapping<TestA, TestB>.Auto(cfg => cfg
                .Bind(x => x.MyPropertyA, x => x.MyPropertyB, cfg => cfg)
                .Bind(x => x.MyPropertyB, x => x.MyPropertyA, cfg => cfg));

            var a = new TestA() { MyPropertyA = 1, MyPropertyB = 2, MyPropertyC = 3};
            var b = new TestB();

            mapping.MapToTarget(a, b);

            Assert.AreEqual(2, b.MyPropertyA);
            Assert.AreEqual(1, b.MyPropertyB);
            Assert.AreEqual(3, b.MyPropertyC);
        }

        [TestMethod]
        public void IgnoredProperties()
        {
            var mapping = Mapping<TestA, TestB>.Auto(cfg => cfg.Ignore(x => x.MyPropertyA));

            var a = new TestA() { MyPropertyA = 11, MyPropertyB = 25 };
            var b = new TestB();

            mapping.Map(a).To(b);

            Assert.AreEqual(0, b.MyPropertyA);
            Assert.AreEqual(25, b.MyPropertyB);
        }

        [TestMethod]
        public void TestMethod1()
        {                
            Mapper.Register(Mapping<TestA, TestB>.Auto());

            var a = new TestA();
            var b = new TestB();

            a.MyPropertyA = 1;
            b.MyPropertyA = 2;
            //config.Map(a).To(b);
            Mapper.MapToTarget(a, b);
            Assert.AreEqual(1, a.MyPropertyA);
            Assert.AreEqual(1, b.MyPropertyA);

            a.MyPropertyA = 3;
            b.MyPropertyA = 4;
            //config.Map(b).To(a);
            Mapper.MapToSource(a, b);
            Assert.AreEqual(4, a.MyPropertyA);
            Assert.AreEqual(4, b.MyPropertyA);
        }

        private class TestA
        {
            public int MyPropertyA { get; set; }

            public int MyPropertyB { get; set; }
        
            public int MyPropertyC { get; set; }
        }

        private class TestB
        {
            public int MyPropertyA { get; set; }
            
            public int MyPropertyB { get; set; }

            public int MyPropertyC { get; set; }

            public string MyPropertyD { get; set; }
        }

        private class TestC
        {
            public TestA MyProperty { get; set; }
        }
    }
}
