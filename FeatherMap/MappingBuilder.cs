using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap
{
    internal class MappingBuilder<TSource, TTarget> : IMappingBuilder<TSource, TTarget>
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

        private static Action<TSource, TTarget> GetSourceToTargetAction<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo, 
            PropertyInfo targetPropertyInfo, 
            PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> config)
        {
            if (config.Direction == Direction.TwoWay || config.Direction == Direction.OneWay)
            {
                if (!targetPropertyInfo.CanWrite)
                    return null;

                var targetSetter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetPropertyInfo);

                return config.Mapping == null
                    ? CreateSimplePropertyMapping<TSource, TTarget, TSourceProperty, TTargetProperty>(
                        sourcePropertyInfo,
                        config.PropertyConverter.Convert,
                        targetSetter)
                    : CreateComplexPropertyMapping<TSource, TTarget, TSourceProperty, TTargetProperty>(
                        sourcePropertyInfo,
                        targetPropertyInfo,
                        targetSetter,
                        config.Mapping.TargetConstructor,
                        config.Mapping.MapToTarget);
            }

            return null;
        }

        private static Action<TFrom, TTo> CreateSimplePropertyMapping<TFrom, TTo, TSourceProperty, TTargetProperty>(
            PropertyInfo fromPropertyInfo,
            Func<TSourceProperty, TTargetProperty> propertyConverter, 
            Action<TTo, TTargetProperty> targetSetter)
        {
            var sourceGetter = PropertyAccess.CreateGetter<TFrom, TSourceProperty>(fromPropertyInfo);
            return (source, target) => targetSetter(target, propertyConverter(sourceGetter(source)));
        }

        

        private static Action<TSource, TTarget> GetTargetToSourceAction<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo, 
            PropertyInfo targetPropertyInfo,
            PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> config)
        {
            if (config.Direction == Direction.TwoWay || config.Direction == Direction.OneWayToSource)
            {
                if (!sourcePropertyInfo.CanWrite)
                    return null;

                var sourceSetter = PropertyAccess.CreateSetter<TSource, TSourceProperty>(sourcePropertyInfo);

                if (config.Mapping == null)
                {
                    return (source, target) => CreateSimplePropertyMapping<TTarget, TSource, TTargetProperty, TSourceProperty>(
                        targetPropertyInfo,
                        config.PropertyConverter.ConvertBack,
                        sourceSetter)(target, source);
                }

                return (source, target) =>
                    CreateComplexPropertyMapping<TTarget, TSource, TTargetProperty, TSourceProperty>(
                        targetPropertyInfo,
                        sourcePropertyInfo,
                        sourceSetter,
                        config.Mapping.SourceConstructor,
                        (s, t) => config.Mapping.MapToSource(t, s))(target, source);
            }

            return null;
        }

        private static Action<TFrom, TTo> CreateComplexPropertyMapping<TFrom, TTo, TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo, 
            Action<TTo, TTargetProperty> targetSetter,
            Func<TTargetProperty> targetCreator,
            Action<TSourceProperty, TTargetProperty> mappingAction)
        {
            var sourceGetter = PropertyAccess.CreateGetter<TFrom, TSourceProperty>(sourcePropertyInfo);
            var targetGetter = PropertyAccess.CreateGetter<TTo, TTargetProperty>(targetPropertyInfo);

            return (source, target) =>
            {
                var targetProperty = targetGetter(target);
                if (targetProperty == null)
                {
                    targetProperty = targetCreator();
                    targetSetter(target, targetProperty);
                }

                mappingAction(sourceGetter(source), targetProperty);
            };
        }
        
        public static Mapping<TSource, TTarget> Auto(Func<AutoPropertyConfig<TSource, TTarget>, AutoPropertyConfig<TSource, TTarget>> cfgFunc)
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);

            var autoConfig = cfgFunc(new AutoPropertyConfig<TSource, TTarget>());
            var mappingBuilder = new MappingBuilder<TSource, TTarget>();
            var mappingBuilderType = typeof(MappingBuilder<TSource, TTarget>);
            var nonGenericBindMethod = mappingBuilderType.GetMethod(nameof(Bind), BindingFlags.NonPublic | BindingFlags.Instance);

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

    }
}
