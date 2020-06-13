using System.Collections.Generic;
using FeatherMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class ListTests
    {
        [TestMethod]
        public void Primitives()
        {
            var primitivesTest = new PrimitivesTest();
            primitivesTest.List = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                primitivesTest.List.Add(i);
            }

            var mapping = Mapping<PrimitivesTest, PrimitivesTest>.Auto();
            var clone = mapping.Clone(primitivesTest);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, clone.List[i]);
            }
        }

        private class PrimitivesTest
        {
            public List<int> List { get; set; }
        }

        private class A
        {
            public List<B> BList { get; set; }
        }

        private class B
        {
            public int Number { get; set; }
        }
    }
}
