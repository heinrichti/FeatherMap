using System;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using FeatherMap.New;
using Nelibur.ObjectMapper;
using Mapper = AutoMapper.Mapper;

namespace BenchmarkCore
{
    [SimpleJob]
    [MemoryDiagnoser]
    public class ReferenceTrackingBenchmark
    {
        private Mapper _autoMapper;
        private Action<A, A> _featherMapNew;

        [GlobalSetup]
        public void Setup()
        {
            _autoMapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<A, A>(); //.PreserveReferences();
            }));

            _featherMapNew = NewMappingBuilder.Auto<A, A>();
            TinyMapper.Bind<A, A>();
            ExpressMapper.Mapper.Register<A, A>();
        }

        [Benchmark]
        public void ExpressMapperBenchmark()
        {
            var a = GetA();
            var b = new A();
            ExpressMapper.Mapper.Map(a, b);
        }

        [Benchmark]
        public void AutoMapper()
        {
            var a = GetA();
            var b = new A();
            _autoMapper.Map(a, b);
        }

        [Benchmark]
        public void FeatherMapNew()
        {
            var a = GetA();
            var b = new A();

            _featherMapNew(a, b);
        }

        private static A GetA()
        {
            var a = new A {Int = 1};
            a.B = new B {Int = 2, C = new C() {A = new A(), Int = 3}};
            return a;
        }


        public class A
        {
            public int Int { get; set; }

            public B B { get; set; }
        }

        public class B
        {
            public int Int { get; set; }
            
            public C C { get; set; }
        }

        public class C
        {
            public int Int { get; set; }

            public A A { get; set; }
        }
    }
}
