namespace FeatherMap.New
{
    public interface IPropertyConverter<in TSource, out TTarget>
    {
        TTarget Convert(TSource source);
    }
}
