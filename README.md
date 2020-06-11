# FeatherMap - Fast and lightweight mapping library

FeatherMap is a fast and lightweight mapping library. The performance is better on all benchmarks that I have tested yet. It does not use any complicated bytecode generation 
or the like so the code should be easy to understand (well, it needs a cleanup, but anyway). The library itself targets .NET Standard 2.0 as well as .NET Framework 4.7.2 and does not have any dependencies on other libraries.
## Basic concepts
The basic concept to know is a `Mapping<TSource, TTarget>`. Mapping an object of class `A` to an object of class `B` is basically copying properties from one object to another while performing some conversions.
A `Mapping<TSource, TTarget>` contains all the information required to map an object of type `TSource` to an object of type `TTarget`. 
You can create an arbitrary number of mappings of the same types if you have use-cases that require different mapping-strategies.

Once created a mapping is immutable and all access is threadsafe.

There is also a global registry for mappings which is faster than any dictionary, see the `Mapper` class. 
But beware, generally I recommend to avoid those global registries. What happens if two maps from `A` to `B` are registered? In this case the last map wins, so no runtime exception.
What if the registry does not contain the mapping? In this case you will most likely get a NullReferenceException.
## Basic Usage
```
public class PersonDto
{
	public int Id;
	public int FirstName;
	public int LastName;
}

public class Person
{
	public int Id;
	public int FirstName;
	public int SurName;
}

var personMapping = Mapping<PersonDto, Person>.New()
	.Bind(dto => dto.Id, x => x.Id)
	.Bind(dto => dto.FirstName, x => x.FirstName)
	.Bind(dto => dto.LastName, x => x.SurName).Build();

var personDto = new PersonDto {Id = 1, FirstName = "Test", LastName = "User"};
var person = new Person();

// maps source to target
personMapping.Map(personDto, person);
```
## Complex objects
Assume this object structure:
```
class A
{
	public int PropA { get; set; }
	public B PropB { get; set; }
}

class B
{
	public int PropBA { get; set; }
}
```
The mapping from `A` to `A` would look like this:
```
var mappingA = Mapping<A, A>.Create(configA => configA
	.Bind(a1 => a1.PropA, a2 => a2.PropB)
	.Bind(a1 => a1.PropB, a2 => a2.PropB, cfg => cfg.CreateMap(configB => configB
		.Bind(b1 => b1.PropBA, b2 => b2.PropBA))));
```
Without `CreateMap` FeatherMap would just copy the reference over instead of properly mapping the object. 

## Automatically created maps
In many cases it is probably good enough to create the mapping automatically by calling `Mapping<A, B>.Auto(config => ...)`. 
This works if the property names are identical or if you specify the bindings manually in the config.
Generally I recommend to avoid auto-mapping, as this may break easily if  you rename properties.

## Reference tracking
A powerful feature of FeatherMap is reference tracking to a void mapping cycles. If the same object was already mapped to the same type, the same target object will be used instead of mapping again.
This feature is enabled by default and will only kick in if FeatherMap detects a cycle in the object graph like this:

```
class A
{
	public B PropB { get; set; }

}

class B
{
	public A PropA { get; set; } // <-- Potential cycle back to A
	public C PropC { get; set; }
}

class Complex
{
	public A PropA { get; set; } // <-- Potential cycle back to A
}

```

As this feature has a performance impact you can manually disable it if you are absolutely certain that there are no reference-loops. Just call `cfg.DisableReferenceTracking()` on the mapping-configuration.

## Type conversion
Sometimes a type conversion has to take place during mapping. 
This can be achieved by using the IPropertyConverter interface and specifying the converter during the creation of the mapping or just giving in a delegate for the mapping:
```
.Bind(x => x.Id, x => x.IdString, cfg => cfg.Convert(i => i.ToString())
```
