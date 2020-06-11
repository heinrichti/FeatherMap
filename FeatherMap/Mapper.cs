namespace FeatherMap
{
    public static class Mapper
    {
        public static void Register<TSource, TTarget>(Mapping<TSource, TTarget> mappingConfiguration)
        {
            MapLookup<TSource, TTarget>.Instance = mappingConfiguration;
        }

        public static void Map<TSource, TTarget>(TSource source, TTarget target)
            => MapLookup<TSource, TTarget>.Instance.Map(source, target);

        private static class MapLookup<TSource, TTarget>
        {
            internal static Mapping<TSource, TTarget> Instance;
        }
    }
}
