# FeatherMap - Fast and lightweight mapping library

FeatherMap is a fast and lightweight mapping library. Its core principles are a little different from other mappers and the performance is better on all benchmarks that I have tested yet. It does not use any complicated bytecode generation or the like so the code should be easy to understand. The library itself uses .NET Standard 2.0 and does not have any dependencies on other libraries.
## Basic concepts
The first basic concept to know is a `Mapping<TSource, TTarget>`. A mapping contains all the information required to map an object of type `TSource` to an object of type `TTarget`. It also contains the information required to map from `TTarget` back to `TSource`. You can create an arbitrary number of mappings of the same types if you have use-cases that require different mapping-strategies. 
Once created a mapping is immutable and all access is threadsafe.
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
personMapping.Map(personDto).To(person);
// maps target to source
personMapping.Map(person).To(personDto);
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
The mapping would look like this:
```
var mappingB = Mapping<B, B>.New()
	.Bind(a =>
var mappingA = Mapping<A, A>.New()
	.Bind(a1 => a1.PropA, a2 => a2.PropB)
	.Bind(a1 => a1.PropB, a2 => a2.PropB, cfg => cfg.UseMapping(mapperB);
```
Without `UseMapping` FeatherMapper would just copy the reference over instead of properly mapping the object. 
This design ensures there are no cyclic mappings. If you need a cyclic mapping you may have to create multiple mappings one where the property that leads to a cycle is ommitted.

## Mapping-Direction
In my use-cases I often have to map properties in one direction, but not the other. This can be achieved by specifying a direction on the mapping.
```
Mapping<A, B>.New()
	.Bind(a => a.CreatedDate, b.CreatedDate, cfg => cfg.MappingDirection(Direction.OneWay).Build();
```

## Type conversion
Sometimes a type conversion has to take place during mapping. This can be achieved by using the IPropertyConverter interface and specifying the converter during the creation of the mapping.
```
class IntToStringConverter : IPropertyConverter<int, string>
{
	public string Convert(int source) => source.ToString();
	public int ConvertBack(string target) => int.Parse(target);
}
```

## Automatically created maps
It is also possible to create the mappings automatically with `Mapping<TSource, TTarget>.Auto()`. This will automatically map all properties with identical names.

I strongly advise against the use of the auto feature as mappings might break, for instance if you rename a property name of one class but not the other.