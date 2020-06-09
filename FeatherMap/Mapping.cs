using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap
{
    public class Mapping<TSource, TTarget>
    {
        private Action<TSource, TTarget> _sourceToTargetFunc;
        private Action<TSource, TTarget> _targetToSourceFunc;

        internal readonly Func<TSource> SourceConstructor;
        internal readonly Func<TTarget> TargetConstructor;

        private Mapping(Action<TSource, TTarget> sourceToTargetFunc, Action<TSource, TTarget> targetToSourceFunc)
        {
            SourceConstructor = GetDefaultConstructor<TSource>();
            TargetConstructor = GetDefaultConstructor<TTarget>();

            _sourceToTargetFunc = sourceToTargetFunc;
            _targetToSourceFunc = targetToSourceFunc;

        }

        public static IMappingBuilder<TSource, TTarget> New() => new MappingBuilder();

        public static Mapping<TSource, TTarget> Auto() => Auto(cfg => cfg);

        public static Mapping<TSource, TTarget> Auto(Func<AutoPropertyConfig<TSource, TTarget>, AutoPropertyConfig<TSource, TTarget>> cfgFunc)
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);

            var autoConfig = cfgFunc(new AutoPropertyConfig<TSource, TTarget>());
            var mappingBuilder = New();
            var mappingBuilderType = mappingBuilder.GetType();
            var nonGenericBindMethod = mappingBuilderType.GetMethod(nameof(MappingBuilder.Bind), BindingFlags.NonPublic | BindingFlags.Instance);

            var properyConfigType = typeof(PropertyConfig<,,,>);

            foreach (var sourceProp in sourceProperties)
            {
                if (autoConfig.PropertiesToIgnore.Contains(sourceProp.Name))
                    continue;

                object config = null;
                string targetPropertyName;
                if (autoConfig.PropertyConfigs.TryGetValue(sourceProp.Name, out var targetConfig))
                {
                    targetPropertyName = targetConfig.TargetPropertyName;
                    config = targetConfig.Config;
                    
                }
                else
                    targetPropertyName = sourceProp.Name;

                if (targetPropertyDictionary.TryGetValue(targetPropertyName, out var targetProp) && sourceProp.PropertyType == targetProp.PropertyType)
                {
                    if (config == null)
                    { 
                        var genericPropertyConfigType = properyConfigType.MakeGenericType(typeof(TSource), typeof(TTarget), sourceProp.PropertyType, targetProp.PropertyType);
                        config = genericPropertyConfigType.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());
                    }

                    var bindMethod = nonGenericBindMethod.MakeGenericMethod(sourceProp.PropertyType, targetProp.PropertyType);                    
                    bindMethod.Invoke(mappingBuilder, new object[] { sourceProp, targetProp, config });
                }
            }

            return mappingBuilder.Build();
        }

        private static Action<TSource, TTarget> GetSourceToTargetAction<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo, 
            PropertyInfo targetPropertyInfo, 
            PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> config)
        {
            Action<TSource, TTarget> sourceToTargetAction = null;
            if (config.Direction == Direction.TwoWay || config.Direction == Direction.OneWay)
            {
                if (!targetPropertyInfo.CanWrite)
                    return null;

                var targetSetter = (Action<TTarget, TTargetProperty>)targetPropertyInfo.GetSetMethod().CreateDelegate(typeof(Action<TTarget, TTargetProperty>));

                if (config.Mapping == null)
                { 
                    var sourceGetter = (Func<TSource, TSourceProperty>)sourcePropertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TSource, TSourceProperty>));
                    var converter = config.PropertyConverter;
                    sourceToTargetAction = (source, target) => targetSetter(target, converter.Convert(sourceGetter(source)));
                }
                else
                {
                    var sourceGetter = (Func<TSource, TSourceProperty>)sourcePropertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TSource, TSourceProperty>));
                    var targetGetter = (Func<TTarget, TTargetProperty>)sourcePropertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TTarget, TTargetProperty>));
                    var targetCreator = config.Mapping.TargetConstructor;

                    sourceToTargetAction = (source, target) =>
                    {
                        var targetProperty = targetGetter(target);
                        if (targetProperty == null)
                        {
                            targetProperty = targetCreator();
                            targetSetter(target, targetProperty);
                        }

                        config.Mapping.MapToTarget(sourceGetter(source), targetProperty);
                    };
                }
            }

            return sourceToTargetAction;
        }

        private static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }

        private static Action<TSource, TTarget> GetTargetToSourceAction<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo, 
            PropertyInfo targetPropertyInfo,
            PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> config)
        {
            Action<TSource, TTarget> targetToSourceAction = null;
            if (config.Direction == Direction.TwoWay || config.Direction == Direction.OneWayToSource)
            {
                if (!sourcePropertyInfo.CanWrite)
                    return null;

                var sourceSetter = (Action<TSource, TSourceProperty>)sourcePropertyInfo.GetSetMethod().CreateDelegate(typeof(Action<TSource, TSourceProperty>));
                
                if (config.Mapping == null)
                {
                    var targetGetter = (Func<TTarget, TTargetProperty>)targetPropertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TTarget, TTargetProperty>));
                    var converter = config.PropertyConverter;
                    targetToSourceAction = (source, target) => sourceSetter(source, converter.ConvertBack(targetGetter(target)));
                }
                else
                {
                    var targetGetter = (Func<TTarget, TTargetProperty>)targetPropertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TTarget, TTargetProperty>));
                    var sourceGetter = (Func<TSource, TSourceProperty>)targetPropertyInfo.GetGetMethod().CreateDelegate(typeof(Func<TSource, TSourceProperty>));
                    var sourceCreator = config.Mapping.SourceConstructor;

                    targetToSourceAction = (source, target) =>
                    {
                        var sourceProperty = sourceGetter(source);
                        if (sourceProperty == null)
                        {
                            sourceProperty = sourceCreator();
                            sourceSetter(source, sourceProperty);
                        }

                        config.Mapping.MapToSource(sourceProperty, targetGetter(target));
                    };
                }
            }

            return targetToSourceAction;
        }

        public (Action<TTarget> To, Action<TTarget> ToTarget) Map(TSource source)
            => (target => MapToTarget(source, target), target => MapToTarget(source, target));

        public (Action<TSource> To, Action<TSource> ToSource) Map(TTarget target) 
            => (source => MapToSource(source, target), source => MapToSource(source, target));

        public void MapToTarget(TSource source, TTarget target) => _sourceToTargetFunc(source, target);

        public void MapToSource(TSource source, TTarget target) => _targetToSourceFunc(source, target);

        private class MappingBuilder : IMappingBuilder<TSource, TTarget>
        {
            private readonly List<(Action<TSource, TTarget> SourceToTarget, Action<TSource, TTarget> TargetToSource)> _mappedProperties = 
                new List<(Action<TSource, TTarget> SourceToTarget, Action<TSource, TTarget> TargetToSource)>();

            public Mapping<TSource, TTarget> Build()
            {
                Action<TSource, TTarget> sourceToTarget = (source, target) => {};
                Action<TSource, TTarget> targetToSource = (source, target) => {};

                using (var enumerator = _mappedProperties.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        sourceToTarget = enumerator.Current.SourceToTarget;
                        targetToSource = enumerator.Current.TargetToSource;
                    }

                    while (enumerator.MoveNext())
                    {
                        sourceToTarget += enumerator.Current.SourceToTarget;
                        targetToSource += enumerator.Current.TargetToSource;
                    }
                }

                return new Mapping<TSource, TTarget>(sourceToTarget, targetToSource);
            }

            internal IMappingBuilder<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
                PropertyInfo sourcePropertyInfo,
                PropertyInfo targetPropertyInfo,
                PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> cfg)
            {
                Action<TSource, TTarget> sourceToTargetAction = GetSourceToTargetAction(sourcePropertyInfo, targetPropertyInfo, cfg);
                Action<TSource, TTarget> targetToSourceAction = GetTargetToSourceAction(sourcePropertyInfo, targetPropertyInfo, cfg);

                _mappedProperties.Add((sourceToTargetAction, targetToSourceAction));

                return this;
            }

            public IMappingBuilder<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
                Expression<Func<TSource, TSourceProperty>> sourceProperty,
                Expression<Func<TTarget, TTargetProperty>> targetProperty)
            {
                return Bind(sourceProperty, targetProperty, cfg => cfg);
            }

            public IMappingBuilder<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
                Expression<Func<TSource, TSourceProperty>> sourceProperty,
                Expression<Func<TTarget, TTargetProperty>> targetProperty,
                Func<PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>, PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>> configFunc)
            {
                var config = configFunc(new PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>());

                var sourceMember = (MemberExpression)sourceProperty.Body;
                var targetMember = (MemberExpression)targetProperty.Body;

                var sourcePropertyInfo = sourceMember.Member.DeclaringType.GetProperty(sourceMember.Member.Name);
                var targetPropertyInfo = targetMember.Member.DeclaringType.GetProperty(targetMember.Member.Name);
                
                return Bind(sourcePropertyInfo, targetPropertyInfo, config);
            }
        }
    }
}
