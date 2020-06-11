using AutoMapper;
using BenchmarkDotNet.Attributes;
using FeatherMap;

namespace BenchmarkCore
{
    [SimpleJob]
    public class CreateOverheadBenchmark
    {
        [Benchmark]
        public void AutomapperBenchmark()
        {
            var mapper = new AutoMapper.Mapper(new MapperConfiguration(config =>
                config.CreateMap<A, A>()));
            mapper.Map<A, A>(new A(), new A());
        }

        [Benchmark]
        public void ExpressmapperBenchmark()
        {
            ExpressMapper.Mapper.Register<A, A>();
            ExpressMapper.Mapper.Map<A, A>(new A(), new A());
        }

        [Benchmark]
        public void FeatherMapBenchmark()
        {
            var map = Mapping<A, A>.Auto();
            map.Map(new A(), new A());
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
