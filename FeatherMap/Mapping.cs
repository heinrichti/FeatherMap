using System;
using System.Linq.Expressions;

namespace FeatherMap
{
    public class Mapping<TSource, TTarget>
    {
        private readonly Action<TSource, TTarget> _sourceToTargetFunc;
        private readonly Action<TSource, TTarget> _targetToSourceFunc;

        internal readonly Func<TSource> SourceConstructor;
        internal readonly Func<TTarget> TargetConstructor;

        internal Mapping(Action<TSource, TTarget> sourceToTargetFunc, Action<TSource, TTarget> targetToSourceFunc)
        {
            SourceConstructor = GetDefaultConstructor<TSource>();
            TargetConstructor = GetDefaultConstructor<TTarget>();

            _sourceToTargetFunc = sourceToTargetFunc;
            _targetToSourceFunc = targetToSourceFunc;

        }

        public static IMappingBuilder<TSource, TTarget> New() => new MappingBuilder<TSource, TTarget>();

        public static Mapping<TSource, TTarget> Auto() => Auto(cfg => cfg);

        public static Mapping<TSource, TTarget> Auto(
            Func<AutoPropertyConfig<TSource, TTarget>, AutoPropertyConfig<TSource, TTarget>> cfgFunc) =>
            MappingBuilder<TSource, TTarget>.Auto(cfgFunc);

        private static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }
        
        public (Action<TTarget> To, Action<TTarget> ToTarget) Map(TSource source)
            => (target => MapToTarget(source, target), target => MapToTarget(source, target));

        public (Action<TSource> To, Action<TSource> ToSource) Map(TTarget target) 
            => (source => MapToSource(source, target), source => MapToSource(source, target));

        public void MapToTarget(TSource source, TTarget target) => _sourceToTargetFunc(source, target);

        public void MapToSource(TSource source, TTarget target) => _targetToSourceFunc(source, target);

        //public static void CompleteAuto() => MappingBuilder<TSource, TTarget>.CompleteAuto();
    }
}
