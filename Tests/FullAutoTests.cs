using System;
using System.Collections.Generic;
using System.Text;
using FeatherMap;
using FeatherMap.New;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class FullAutoTests
    {
        [TestMethod]
        public void FullAuto()
        {
            var action = NewMappingBuilder.CreateMap<TestA, TestA>();
            
            var a = new TestA() {IntTest = 12};
            a.TestB = new TestB {StringTest = "Hello world"};

            var b = new TestA {TestB = new TestB()};

            action(a, b);
        }

        private class TestA
        {
            public int IntTest { get; set; }

            public TestB TestB { get; set; }
        }

        private class TestB
        {
            public string StringTest { get; set; }
        }
    }
}
