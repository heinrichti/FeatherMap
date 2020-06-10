using System;

namespace BenchmarkCore
{
    public class Person
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
    }
}
