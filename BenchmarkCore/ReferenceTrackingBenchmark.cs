using System;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using FeatherMap;
using Mapster;
using Nelibur.ObjectMapper;
using Mapper = AutoMapper.Mapper;

namespace BenchmarkCore
{
    [SimpleJob(warmupCount:3, targetCount:3)]
    [MemoryDiagnoser]
    public class ReferenceTrackingBenchmark
    {
        private Mapper _autoMapper;
        private Mapping<A, A> _featherMapNew;
        private MapsterMapper.Mapper _mapsterMapper;
        private TypeAdapterConfig _typeAdapterConfig;

        [GlobalSetup]
        public void Setup()
        {
            _autoMapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<A, A>().PreserveReferences();
            }));

            _featherMapNew = Mapping<A, A>.Auto();
            TinyMapper.Bind<A, A>();
            ExpressMapper.Mapper.Register<A, A>();

            var typeAdapterSetter = TypeAdapterConfig<A, A>
                .NewConfig()
                .PreserveReference(true);
            typeAdapterSetter.Config.Compile();
            _mapsterMapper = new MapsterMapper.Mapper(typeAdapterSetter.Config);

            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
        }

        [Benchmark]
        public void Mapster()
        {
            var a = GetA();
            var b = new A();

            _mapsterMapper.Map(a, b);
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
        public void FeatherMapBenchmark()
        {
            var a = GetA();
            var b = new A();

            _featherMapNew.Map(a, b);
        }

        private static A GetA()
        {
            var a = new A {Int = 1};
            a.B = new B {Int = 2, C = new C() {Int = 3}};
            a.B.C.B = a.B;
            a.B.C.A = a;
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

            public B B { get; set; }

            public A A { get; set; }
        }
    }
}
