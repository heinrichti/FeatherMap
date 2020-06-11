using System;
using System.Collections.Generic;
using System.Text;
using FeatherMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class NewMappingTest
    {
        [TestMethod]
        public void CreateMapping()
        {
            //var mapping = NewMapping.Create<A, A>(map => map
            //    .Bind(x => x.Id, a => a.Id)
            //    .Bind(a => a.TestA, a => a.TestA)
            //    .Bind(a => a.B, a => a.B, config => config.CreateMap(bMap =>
            //        bMap.Bind(b => b.IdString, b => b.IdString, cfg => cfg.Convert(s => s + "321")))));

            var mapping = Mapping<A, A>.Auto(x => x
                .Bind(a => a.TestA, a => a.TestA, x => x.Convert(i => i + 25)));

            var a1 = new A();
            a1.Id = Guid.NewGuid();
            a1.TestA = 12;
            a1.B = new B() {IdString = "Test123"};
            a1.B.A = a1;

            var a2 = new A();
            mapping.Map(a1, a2);
        }

        private class A
        {
            public int TestA { get; set; }

            public Guid Id { get; set; }

            public B B { get; set; }
        }

        private class B
        {
            public string IdString { get; set; }

            public A A { get; set; }

        }
    }
}
