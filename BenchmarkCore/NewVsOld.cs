using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FeatherMap;

namespace BenchmarkCore
{
    [SimpleJob(RunStrategy.Throughput, 1, 2, 1)]
    [MemoryDiagnoser]
    public class NewVsOld
    {
        private Mapping<Person, Person> _personMapping;
        private Guid _guid;

        [GlobalSetup]
        public void Setup()
        {
            _personMapping = Mapping<Person, Person>.Auto();
            _guid = Guid.NewGuid();
        }

        [Benchmark]
        public void Old()
        {
            var person = new Person()
                {Address = new Address() {Street = "Testavenue"}, FirstName = "Tim", LastName = "User", Id = _guid};
        }

        [Benchmark]
        public void New()
        {
            var person = new Person()
                {Address = new Address() {Street = "Testavenue"}, FirstName = "Tim", LastName = "User", Id = _guid};

            var b = new Person();
            _personMapping.Map(person, b);
        }
    }
}
