using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using ExpressMapper;
using FeatherMap;
using Mapper = AutoMapper.Mapper;

namespace BenchmarkCore
{
    [SimpleJob(warmupCount:3, targetCount:3)]
    [MemoryDiagnoser]
    public class ListBenchmark
    {
        private Mapper _autoMapper;
        private Mapping<Complex, Complex> _mapping;

        [GlobalSetup]
        public void Setup()
        {
            _autoMapper = new AutoMapper.Mapper(new MapperConfiguration(cfg => cfg.CreateMap<Complex, Complex>()));
            _mapping = Mapping<Complex, Complex>.Auto();
            ExpressMapper.Mapper.Instance.Register<Complex2, Complex2>();
            ExpressMapper.Mapper.Instance.Register<Complex, Complex>();
        }

        [Benchmark]
        public void AutoMapper()
        {
            var item = GetItem();
            _autoMapper.Map<Complex, Complex>(item);
        }

        [Benchmark]
        public void ExpressMapperBenchmark()
        {
            var item = GetItem();
            ExpressMapper.Mapper.Instance.Map<Complex, Complex>(item);
        }

        [Benchmark]
        public void FeatherMap()
        {
            var item = GetItem();
            _mapping.Clone(item);
        }

        private static Complex GetItem()
        {
            var complex = new Complex();
            complex.Objects = new List<Complex2>();
            for (int i = 0; i < 10; i++)
            {
                complex.Objects.Add(new Complex2() {I = i, Str = i.ToString()});
            }

            return complex;
        }

        private class Complex
        {
            public List<Complex2> Objects { get; set; }
        }

        private class Complex2
        {
            public string Str { get; set; }

            public int I { get; set; }
        }


    }
}
