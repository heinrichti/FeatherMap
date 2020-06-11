using System;

namespace FeatherMap
{
    public class Mapping<TSource, TTarget>
    {
        private readonly Action<TSource, TTarget> _mapAction;
        private readonly Func<TTarget> _targetCreator;

        internal Mapping(Action<TSource, TTarget> mapAction)
        {
            _mapAction = mapAction;
            _targetCreator = PropertyAccess.GetDefaultConstructor<TTarget>();
        }

        public TTarget Clone(TSource source)
        {
            var target = _targetCreator();
            _mapAction(source, target);
            return target;
        }

        public void Map(TSource source, TTarget target)
        {
            _mapAction(source, target);
        }

        public static Mapping<TSource, TTarget> Create(Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgAction)
        {
            var mapAction = MappingBuilder.Create(cfgAction);
            return new Mapping<TSource, TTarget>(mapAction);
        }

        public static Mapping<TSource, TTarget> Auto()
        {
            var mapAction = MappingBuilder.Auto<TSource, TTarget>(configuration => configuration);
            return new Mapping<TSource, TTarget>(mapAction);
        }

        public static Mapping<TSource, TTarget> Auto(
            Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgFunc)
        {
            var mapAction = MappingBuilder.Auto<TSource, TTarget>(configuration => cfgFunc(configuration));
            return new Mapping<TSource, TTarget>(mapAction);
        }
    }
}