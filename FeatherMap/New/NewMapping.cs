using System;

namespace FeatherMap.New
{
    public class NewMapping<TSource, TTarget>
    {
        private readonly Action<TSource, TTarget> _mapAction;
        private readonly Func<TTarget> _targetCreator;

        internal NewMapping(Action<TSource, TTarget> mapAction)
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

        public static NewMapping<TSource, TTarget> Create(Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgAction)
        {
            var mapAction = NewMappingBuilder.Create(cfgAction);
            return new NewMapping<TSource, TTarget>(mapAction);
        }

        public static NewMapping<TSource, TTarget> Auto()
        {
            var mapAction = NewMappingBuilder.Auto<TSource, TTarget>(configuration => configuration);
            return new NewMapping<TSource, TTarget>(mapAction);
        }

        public static NewMapping<TSource, TTarget> Auto(
            Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgFunc)
        {
            var mapAction = NewMappingBuilder.Auto<TSource, TTarget>(configuration => cfgFunc(configuration));
            return new NewMapping<TSource, TTarget>(mapAction);
        }
    }
}