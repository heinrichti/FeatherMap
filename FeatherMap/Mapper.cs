namespace FeatherMap
{
    public static class Mapper
    {
        public static void Register<TSource, TTarget>(Mapping<TSource, TTarget> mappingConfiguration)
        {
            MapLookup<TSource, TTarget>.Instance = mappingConfiguration;
        }

        public static void MapToSource<TSource, TTarget>(TSource source, TTarget target)
        {
            MapLookup<TSource, TTarget>.Instance.MapToSource(source, target);
        }

        public static void MapToTarget<TSource, TTarget>(TSource source, TTarget target)
        {
            MapLookup<TSource, TTarget>.Instance.MapToTarget(source, target);
        }

        private static class MapLookup<TSource, TTarget>
        {
            internal static Mapping<TSource, TTarget> Instance;
        }
    }
}
