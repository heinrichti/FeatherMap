﻿using FeatherMap.New;
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
            a.TestB = new TestB {StringTest = "Hello world", ARef = new TestA() {IntTest = 2, TestB = new TestB(){StringTest = "ululu"}}};

            var b = new TestA();

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

            public TestA ARef { get; set; }
        }
    }
}