namespace FeatherMap
{
    public interface IPropertyConverter<TSourceProperty, TTargetProperty>
    {
        TTargetProperty Convert(TSourceProperty source);

        TSourceProperty ConvertBack(TTargetProperty target);
    }
}
