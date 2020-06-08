namespace FeatherMap
{
    internal class EmptyPropertyConverter<TProperty> : IPropertyConverter<TProperty, TProperty>
    {
        public TProperty Convert(TProperty source) => source;

        public TProperty ConvertBack(TProperty target) => target;
    }
}