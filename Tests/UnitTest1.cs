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
            var mappingC = Mapping<TestC, TestC>.Auto();

            var a = new TestC { MyProperty = new TestA { MyPropertyA = 1, MyPropertyB = 2, MyPropertyC = 3} };
            var b = new TestC();

            mappingC.Map(a, b);

            Assert.AreEqual(1, b.MyProperty.MyPropertyA);
            Assert.AreEqual(2, b.MyProperty.MyPropertyB);
            Assert.AreEqual(3, b.MyProperty.MyPropertyC);
        }

        [TestMethod]
        public void ConverterNotProvided()
        {
            Assert.ThrowsException<ArgumentException>(() => Mapping<TestA, TestB>.Create(cfg => 
                cfg.Bind(x => x.MyPropertyA, x => x.MyPropertyD)));
        }

        [TestMethod]
        public void Converter()
        {
            var a = new TestB() { MyPropertyA = 1, MyPropertyD = "hello" };
            var b = new TestB();
            var mapping = Mapping<TestB, TestB>.Create(config => config
                .Bind(x => x.MyPropertyA, x => x.MyPropertyD, 
                    cfg => cfg.Convert(i => i.ToString())));

            mapping.Map(a, b);

            Assert.AreEqual("1", b.MyPropertyD);
        }

        [TestMethod]
        public void AutoWithOverriddenMembers()
        {
            var mapping = Mapping<TestA, TestB>.Auto(cfg => cfg
                .Bind(x => x.MyPropertyA, x => x.MyPropertyB)
                .Bind(x => x.MyPropertyB, x => x.MyPropertyA));

            var a = new TestA() { MyPropertyA = 1, MyPropertyB = 2, MyPropertyC = 3};
            var b = new TestB();

            mapping.Map(a, b);

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

            mapping.Map(a, b);

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
            Mapper.Map(a, b);
            Assert.AreEqual(1, a.MyPropertyA);
            Assert.AreEqual(1, b.MyPropertyA);
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
