using FeatherMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class FullAutoTests
    {
        [TestMethod]
        public void FullAuto()
        {
            var mapping = Mapping<TestA, TestA>.Auto();
            
            var a = new TestA() {IntTest = 12};
            a.TestB = new TestB {StringTest = "Hello world", ARef = a};

            var b = mapping.Clone(a);

            Assert.AreEqual(12, b.IntTest);
            Assert.AreEqual("Hello world", b.TestB.StringTest);
        }

        private class TestA
        {
            public int IntTest { get; set; }

            public TestB TestB { get; set; }
        }

        private class TestB
        {
            public string StringTest { get; set; }

            public TestA ARef { get; set; }
        }
    }
}
