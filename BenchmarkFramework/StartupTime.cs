using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatherMap;

namespace BenchmarkFramework
{
    [SimpleJob]
    public class StartupTime
    {
        [Benchmark]
        public void StartupTimeBenchmark()
        {
            //MappingConfiguration<Person, Person>.New()
            //    .Bind(x => x.Id, x => x.Id)
            //    .Bind(x => x.FirstName, x => x.FirstName)
            //    .Bind(x => x.LastName, x => x.LastName).Build();
            Mapping<Person, Person>.Auto();
        }

        [Benchmark]
        public void AutoMapper()
        {
            new AutoMapper.MapperConfiguration(cfg => cfg.CreateMap<Person, Person>()).CreateMapper();
        }
    }
}
