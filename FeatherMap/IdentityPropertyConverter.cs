namespace FeatherMap
{
    internal sealed class IdentityPropertyConverter<T> : IPropertyConverter<T, T>
    {
        public static IdentityPropertyConverter<T> Instance = new IdentityPropertyConverter<T>();

        public T Convert(T source) => source;
    }
}
