using System;
using System.Collections.Generic;
using System.Text;
using FeatherMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class ReferenceTrackingTests
    {
        [TestMethod]
        public void ReferenceTracking()
        {
            var a = new A();
            var b = new B();
            var c = new C();

            a.B = b;
            b.A = a;
            b.C = c;
            c.A = a;
            c.C1 = c;

            var mapping = Mapping<A, A>.Auto();
            var clone = mapping.Clone(a);

            Assert.AreSame(clone, clone.B.A);
            Assert.AreSame(clone.B.C, clone.B.C.C1);
            Assert.AreSame(clone, clone.B.C.A);
        }

        private class A
        {
            public B B { get; set; }
        }

        private class B
        {
            public A A { get; set; }
            public C C { get; set; }
        }

        private class C
        {
            public A A { get; set; }
            public C C1 { get; set; }
        }
    }
}
