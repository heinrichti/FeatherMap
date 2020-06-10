using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Nelibur.ObjectMapper;
using System;
using System.Threading.Tasks;
using FeatherMap;

namespace BenchmarkCore
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    public class Program
    {
        static void Main(string[] args)
        {
            var newVsOld = new NewVsOld();
            newVsOld.Setup();
            newVsOld.Old();
            newVsOld.New();

            //BenchmarkRunner.Run<Program>();
            //BenchmarkRunner.Run<StartupTime>();
            //BenchmarkRunner.Run<GettersSetters>();
            BenchmarkRunner.Run<NewVsOld>();
            //BenchmarkRunner.Run<ContructorBenchmark>();
        }

        private Person _personA;
        private Person _personB;
        private AutoMapper.IMapper _autoMapper;

        //[GlobalSetup]
        //public void Setup()
        //{
        //    _personA = new Person {Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address {Street = "Testavenue"}};
        //    _personB = new Person();

        //    var adressMapping = Mapping<Address, Address>.Auto();
        //    Mapper.Register(Mapping<Person, Person>.Auto(cfg =>
        //        cfg.Direction(Direction.OneWay)
        //            .Bind(x => x.Address, person => person.Address, config => config.UseMapping(adressMapping))));

        //    _autoMapper = new AutoMapper.MapperConfiguration(cfg =>
        //    {
        //        cfg.CreateMap<Address, Address>();
        //        cfg.CreateMap<Person, Person>();
        //    }).CreateMapper();

        //    TinyMapper.Bind<Person, Person>();

        //    ExpressMapper.Mapper.Register<Person, Person>();
        //}

        //[Benchmark]
        //public void TinyMapperBenchmark()
        //{
        //    _personA = new Person {Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address {Street = "Testavenue"}};
        //    _personB = new Person();

        //    TinyMapper.Map(_personA, _personB);
        //}

        //[Benchmark]
        //public void AutoMapperBenchmark()
        //{
        //    _personA = new Person {Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address {Street = "Testavenue"}};
        //    _personB = new Person();

        //    _autoMapper.Map(_personA, _personB);
        //}

        //[Benchmark]
        //public void FeatherMapBenchmark()
        //{
        //    _personA = new Person {Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address {Street = "Testavenue"}};
        //    _personB = new Person();

        //    Mapper.MapToTarget(_personA, _personB);
        //}

        //[Benchmark]
        //public void ExpressMapperBenchmark()
        //{
        //    _personA = new Person {Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address {Street = "Testavenue"}};
        //    _personB = new Person();

        //    ExpressMapper.Mapper.Map(_personA, _personB);
        //}

        //[Benchmark]
        //public void Handwritten()
        //{
        //    _personB.Id = _personA.Id;
        //    _personB.FirstName = _personA.FirstName;
        //    _personB.LastName = _personA.LastName;
        //    Address adr = _personB.Address;
        //    if (adr == null)
        //        adr = _personB.Address = new Address();

        //    adr.Street = _personA.Address.Street;
        //}
    }
}
