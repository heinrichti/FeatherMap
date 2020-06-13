using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Nelibur.ObjectMapper;
using System;
using System.Threading.Tasks;
using FeatherMap;
using Mapster;

namespace BenchmarkCore
{
    [SimpleJob(warmupCount:3, targetCount:3)]
    [MemoryDiagnoser]
    public class Program
    {
        static async Task Main(string[] args)
        {
            //var newVsOld = new NewVsOld();
            //newVsOld.Setup();
            //newVsOld.Old();
            //newVsOld.New();

            var referenceTrackingBenchmark = new ReferenceTrackingBenchmark();
            referenceTrackingBenchmark.Setup();
            referenceTrackingBenchmark.FeatherMapBenchmark();
            referenceTrackingBenchmark.AutoMapper();
            referenceTrackingBenchmark.Mapster();

            //await Task.Delay(5000);

            //referenceTrackingBenchmark.AutoMapper();
            //referenceTrackingBenchmark.ExpressMapperBenchmark();
            //for (int i = 0; i < 100000000; i++)
            //{
            //    referenceTrackingBenchmark.FeatherMapNew();
            //}

            //Console.WriteLine("Starting");

            //var trackingBenchmark = new ReferenceTrackingBenchmark();
            //trackingBenchmark.Setup();
            //for (int i = 0; i < 100000000; i++)
            //{
            //    trackingBenchmark.FeatherMapNew();
            //}

            //Console.WriteLine("Ended");

            //await Task.Delay(5000);


            //BenchmarkRunner.Run<Program>();
            BenchmarkRunner.Run<ReferenceTrackingBenchmark>();
            //BenchmarkRunner.Run<StartupTime>();
            //BenchmarkRunner.Run<GettersSetters>();
            //BenchmarkRunner.Run<ContructorBenchmark>();
            //BenchmarkRunner.Run<CreateOverheadBenchmark>();
            //BenchmarkRunner.Run<ListBenchmark>();
        }

        private Person _personA;
        private Person _personB;
        private AutoMapper.IMapper _autoMapper;
        private MapsterMapper.Mapper _mapsterMapper;

        [GlobalSetup]
        public void Setup()
        {
            _personA = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address { Street = "Testavenue" } };
            _personB = new Person();

            Mapper.Register(Mapping<Person, Person>.Auto());

            _autoMapper = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Address, Address>();
                cfg.CreateMap<Person, Person>();
            }).CreateMapper();

            TinyMapper.Bind<Person, Person>();

            ExpressMapper.Mapper.Register<Person, Person>();

            var typeAdapterSetter = TypeAdapterConfig<Person, Person>.NewConfig();
            typeAdapterSetter.Config.Compile();
            _mapsterMapper = new MapsterMapper.Mapper(typeAdapterSetter.Config);
        }

        [Benchmark]
        public void Mapster()
        {
            _personA = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address { Street = "Testavenue" } };
            _personB = new Person();

            _mapsterMapper.Map(_personA, _personB);
        }

        [Benchmark]
        public void TinyMapperBenchmark()
        {
            _personA = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address { Street = "Testavenue" } };
            _personB = new Person();

            TinyMapper.Map(_personA, _personB);
        }

        [Benchmark]
        public void AutoMapperBenchmark()
        {
            _personA = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address { Street = "Testavenue" } };
            _personB = new Person();

            _autoMapper.Map(_personA, _personB);
        }

        [Benchmark]
        public void FeatherMapBenchmark()
        {
            _personA = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address { Street = "Testavenue" } };
            _personB = new Person();

            Mapper.Map(_personA, _personB);
        }

        [Benchmark]
        public void ExpressMapperBenchmark()
        {
            _personA = new Person { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Address = new Address { Street = "Testavenue" } };
            _personB = new Person();

            ExpressMapper.Mapper.Map(_personA, _personB);
        }

        [Benchmark]
        public void Handwritten()
        {
            _personB.Id = _personA.Id;
            _personB.FirstName = _personA.FirstName;
            _personB.LastName = _personA.LastName;
            Address adr = _personB.Address;
            if (adr == null)
                adr = _personB.Address = new Address();

            adr.Street = _personA.Address.Street;
        }
    }
}
