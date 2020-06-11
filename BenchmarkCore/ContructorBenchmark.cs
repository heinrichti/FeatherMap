using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace BenchmarkCore
{
    [SimpleJob]
    public class ContructorBenchmark
    {
        private Func<Address> _expressionsConstructor;
        private Func<Address> _activatorConstructor;

        [GlobalSetup]
        public void Setup()
        {
            _expressionsConstructor = GetDefaultConstructor<Address>();
            _activatorConstructor = ActivatorConstructor<Address>();
        }

        [Benchmark]
        public void Expressions()
        {
            var tmp = _expressionsConstructor();
        }

        [Benchmark]
        public void ActivatorConstructor()
        {
            var tmp = _activatorConstructor();
        }

        private static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }

        private static Func<T> ActivatorConstructor<T>() => () => Activator.CreateInstance<T>();
    }
}
