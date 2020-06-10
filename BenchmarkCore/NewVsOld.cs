using System;
using BenchmarkDotNet.Attributes;
using FeatherMap;
using FeatherMap.New;

namespace BenchmarkCore
{
    [SimpleJob]
    [MemoryDiagnoser]
    public class NewVsOld
    {
        private Mapping<Person, Person> _personMapping;
        private Action<Person, Person> _personMappingNew;
        private Guid _guid;

        [GlobalSetup]
        public void Setup()
        {
            var addressMapping = Mapping<Address, Address>.Auto();
            _personMapping = Mapping<Person, Person>.Auto(cfg => 
                cfg.Bind(x => x.Address, x => x.Address, config => config.UseMapping(addressMapping)));

            _personMappingNew = NewMappingBuilder.Auto<Person, Person>();
            _guid = Guid.NewGuid();
        }

        [Benchmark]
        public void Old()
        {
            var person = new Person()
                {Address = new Address() {Street = "Testavenue"}, FirstName = "Tim", LastName = "User", Id = _guid};

            _personMapping.MapToTarget(person, new Person());
        }

        [Benchmark]
        public void New()
        {
            var person = new Person()
                {Address = new Address() {Street = "Testavenue"}, FirstName = "Tim", LastName = "User", Id = _guid};

            var b = new Person();
            _personMappingNew(person, b);
        }
    }
}
