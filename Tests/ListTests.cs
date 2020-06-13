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
            //var mapping = Mapping<PrimitivesTest, PrimitivesTest>.Create(cfg => cfg
            //    .Bind(x => x.List, x => x.List));
            var clone = mapping.Clone(primitivesTest);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, clone.List[i]);
            }
        }

        [TestMethod]
        public void ComplexTest()
        {
            var mapping = Mapping<Complex, Complex>.Auto();
            var a = new Complex();
            a.Objects = new List<Complex2>();
            for (int i = 0; i < 10; i++)
            {
                a.Objects.Add(new Complex2() {Str = i.ToString()});
            }

            var complex = mapping.Clone(a);
            Assert.IsNotNull(complex.Objects);
            for (int i = 0; i < complex.Objects.Count; i++)
            {
                Assert.AreEqual(i.ToString(), complex.Objects[i].Str);
            }
        }

        private class Complex
        {
            public List<Complex2> Objects { get; set; }
        }

        private class Complex2
        {
            public string Str { get; set; }
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
