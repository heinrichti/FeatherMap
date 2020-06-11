namespace FeatherMap
{
    public interface IPropertyConverter<in TSource, out TTarget>
    {
        TTarget Convert(TSource source);
    }
}
