using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FeatherMap
{
    internal class MappingBuilder
    {
        public static Action<TSource, TTarget> Create<TSource, TTarget>(
            Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgAction)
        {
            var mappingConfiguration = new MappingConfiguration<TSource, TTarget>();
            mappingConfiguration = cfgAction(mappingConfiguration);
            var result = CreateMap(typeof(TSource), typeof(TTarget), mappingConfiguration, new Dictionary<SourceToTargetMap, MapTracker>());

            if (result.ReferenceTrackingTypes.Any())
            {
                Action<TSource, TTarget> ReferenceTrackedMap(Action<TSource, TTarget, ReferenceTracker> mappingFunc) =>
                    (source, target) =>
                    {
                        var referenceTracker = new ReferenceTracker();
                        referenceTracker.Add(typeof(TTarget), source, target);
                        mappingFunc(source, target, referenceTracker);
                    };

                return ReferenceTrackedMap(result.MappingFunc);
            }

            Func<Action<TSource, TTarget, ReferenceTracker>, Action<TSource, TTarget>> f =
                mappingAction => (s1, s2) => mappingAction(s1, s2, new ReferenceTracker());

            return f(result.MappingFunc);
        }

        private static ComplexMapResult<TSource, TTarget> CreateMap<TSource, TTarget>(
            Type sourceType, Type targetType,
            MappingConfiguration<TSource, TTarget> config,
            Dictionary<SourceToTargetMap, MapTracker> existingMaps)
        {
            var referenceTrackingMapping = new MapTracker();

            if (config.ReferenceTrackingEnabled)
            {
                if (existingMaps.TryGetValue(new SourceToTargetMap(sourceType, targetType), out var m))
                {
                    // reuse existing map to avoid Stackoverflow
                    return new ComplexMapResult<TSource, TTarget>((source, target, tracker) =>
                        {
                            var action = (Action<TSource, TTarget, ReferenceTracker>) m.Action;
                            action(source, target, tracker);
                        },
                        new List<ReferenceTrackingType> {new ReferenceTrackingType(sourceType, targetType)});}
            }

            var simplePropertyMapMethod = typeof(MappingBuilder).GetMethod(nameof(CreateSimplePropertyMap), BindingFlags.Static | BindingFlags.NonPublic);
            var complexPropertyMapMethod = typeof(MappingBuilder).GetMethod(nameof(CreateComplexMap), BindingFlags.Static | BindingFlags.NonPublic);

            existingMaps.Add(new SourceToTargetMap(sourceType, targetType), referenceTrackingMapping);

            var referenceTrackingTypes = new List<ReferenceTrackingType>();
            var actions = new List<Action<TSource, TTarget, ReferenceTracker>>();

            foreach (var propertyMap in config.PropertyMaps)
            {
                if (propertyMap.HasMappingConfiguration())
                {
                    var createMapFunc = complexPropertyMapMethod.MakeGenericMethod(
                        sourceType, propertyMap.SourcePropertyInfo.PropertyType, 
                        targetType, propertyMap.TargetPropertyInfo.PropertyType);
                    var complexPropertyMapping = (ComplexMapResult<TSource, TTarget>) createMapFunc.Invoke(null,
                        new[] {propertyMap.SourcePropertyInfo, propertyMap.TargetPropertyInfo, existingMaps, propertyMap.ConfigObject});
                    referenceTrackingTypes.AddRange(complexPropertyMapping.ReferenceTrackingTypes);
                    actions.Add(complexPropertyMapping.MappingFunc);

                }
                else
                {
                    var mapFuncCreator = simplePropertyMapMethod.MakeGenericMethod(
                        sourceType, propertyMap.SourcePropertyInfo.PropertyType,
                        targetType, propertyMap.TargetPropertyInfo.PropertyType);
                    var map = (Action<TSource, TTarget, ReferenceTracker>)mapFuncCreator.Invoke(null, new[]
                        {propertyMap.SourcePropertyInfo, propertyMap.TargetPropertyInfo, propertyMap.ConfigObject});
                    actions.Add(map);
                }
            }

            var result = actions.Aggregate((accumulate, action) => accumulate + action);
            referenceTrackingMapping.Action = result;
            return new ComplexMapResult<TSource, TTarget>(result, referenceTrackingTypes);
        }

        public static Action<TSource, TTarget> Auto<TSource, TTarget>(Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgFunc)
        {
            var cfg = new MappingConfiguration<TSource, TTarget>();
            cfg = cfgFunc(cfg);

            var mappingConfigurations = new Dictionary<SourceToTargetMap, object>();

            return Create<TSource, TTarget>(configuration =>
                AutoConfig(mappingConfigurations, cfg));
        }

        internal static MappingConfiguration<TSource, TTarget> AutoConfig<TSource, TTarget>(
            Dictionary<SourceToTargetMap, object> typeConfigs, MappingConfiguration<TSource, TTarget> mappingConfiguration)
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            if (typeConfigs.TryGetValue(new SourceToTargetMap(sourceType, targetType), out var previousMappingConfig))
                return (MappingConfiguration<TSource, TTarget>) previousMappingConfig;

            if (mappingConfiguration == null)
                mappingConfiguration = new MappingConfiguration<TSource, TTarget>();

            typeConfigs.Add(new SourceToTargetMap(sourceType, targetType), mappingConfiguration);

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);

            foreach (var sourceProperty in sourceProperties)
            {
                if (mappingConfiguration.PropertiesToIgnore.Contains(sourceProperty.Name))
                    continue;

                if (targetPropertyDictionary.TryGetValue(sourceProperty.Name, out var targetProperty))
                {
                    if (!targetProperty.CanWrite)
                        continue;

                    if (sourceProperty.PropertyType.IsPrimitive || sourceProperty.PropertyType == typeof(string) ||
                        sourceProperty.PropertyType.IsValueType)
                    {
                        // TODO evtl gibt es dafür bereits eine config
                        var bindFunc = mappingConfiguration.GetType().GetMethod("BindInternalWithoutConfig",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                            .MakeGenericMethod(sourceProperty.PropertyType, targetProperty.PropertyType);

                        bindFunc.Invoke(mappingConfiguration, new object[] {sourceProperty, targetProperty});
                    }
                    else
                    {
                        var bindFunc = typeof(MappingBuilder).GetMethod(nameof(BindComplexConfig),
                                BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(typeof(TSource), typeof(TTarget), sourceProperty.PropertyType, targetProperty.PropertyType);

                        bindFunc.Invoke(null, new object[] {sourceProperty, targetProperty, mappingConfiguration, typeConfigs});
                    }
                }
            }

            return mappingConfiguration;
        }

        private static void BindComplexConfig<TSource, TTarget, TSourceProperty, TTargetProperty>(
            PropertyInfo sourceProperty, PropertyInfo targetProperty, 
            MappingConfiguration<TSource, TTarget> mappingConfiguration,
            Dictionary<SourceToTargetMap, object> typeMappings)
        {
            mappingConfiguration.BindInternal(sourceProperty, targetProperty,
                new PropertyConfig<TSourceProperty, TTargetProperty>()
                    .CreateMap(x => 
                        AutoConfig(typeMappings, 
                            mappingConfiguration.GetChildConfigOrNew<TSourceProperty, TTargetProperty>(sourceProperty, targetProperty))));
        }

        private static ComplexMapResult<TSource, TTarget> CreateComplexMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            Dictionary<SourceToTargetMap, MapTracker> typeMappings,
            PropertyConfig<TSourceProperty, TTargetProperty> config)
            where TSource : class
            where TTarget : class
            where TSourceProperty : class
            where TTargetProperty : class
        {
            var createMapFunc = typeof(MappingBuilder).GetMethod(nameof(CreateMap), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(sourceProperty.PropertyType, targetProperty.PropertyType);

            var mapFuncResult = (ComplexMapResult<TSourceProperty, TTargetProperty>)createMapFunc
                .Invoke(null, new object[] { sourceProperty.PropertyType, targetProperty.PropertyType, config.MappingConfiguration, typeMappings });
            var mapFunc = mapFuncResult.MappingFunc;
            var constructor = PropertyAccess.GetDefaultConstructor<TTargetProperty>();

            var sourcePropertyGetter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            var requiresReferenceTracking = mapFuncResult.ReferenceTrackingTypes;

            if (!config.MappingConfiguration.ReferenceTrackingEnabled || requiresReferenceTracking.Any(x => 
                x.Source == sourceProperty.PropertyType && x.Target == targetProperty.PropertyType))
                return new ComplexMapResult<TSource, TTarget>(MappingFuncWithReferenceTracking(sourcePropertyGetter, setter, constructor, mapFunc), 
                    requiresReferenceTracking);

            return new ComplexMapResult<TSource, TTarget>(
                MappingWithoutReferenceTracking(sourcePropertyGetter, setter, constructor, mapFunc), 
                requiresReferenceTracking);
        }

        private static Action<TSource, TTarget, ReferenceTracker> MappingFuncWithReferenceTracking<TSource, TTarget,
            TSourceProperty, TTargetProperty>(
            Func<TSource, TSourceProperty> sourceGetter,
            Action<TTarget, TTargetProperty> targetSetter,
            Func<TTargetProperty> targetConstructor,
            Action<TSourceProperty, TTargetProperty, ReferenceTracker> mappingFunc)
            where TSource : class
            where TTarget : class
            where TSourceProperty : class
            where TTargetProperty : class =>
            (source, target, referenceTracker) =>
            {
                var sourceValue = sourceGetter(source);

                if (sourceValue == null)
                {
                    targetSetter(target, null);
                    return;
                }

                if (referenceTracker.TryGet(typeof(TTargetProperty), sourceValue, out var alreadyMappedObject))
                {
                    targetSetter(target, (TTargetProperty) alreadyMappedObject);
                    return;
                }

                var targetProp = targetConstructor();
                referenceTracker.Add(typeof(TTargetProperty), sourceValue, targetProp);

                targetSetter(target, targetProp);
                mappingFunc(sourceValue, targetProp, referenceTracker);
            };

        private static Action<TSource, TTarget, ReferenceTracker> MappingWithoutReferenceTracking<TSource, TTarget,
            TSourceProperty, TTargetProperty>(
            Func<TSource, TSourceProperty> sourceGetter,
            Action<TTarget, TTargetProperty> targetSetter,
            Func<TTargetProperty> targetConstructor,
            Action<TSourceProperty, TTargetProperty, ReferenceTracker> mappingFunc)
            where TSource : class
            where TTarget : class
            where TSourceProperty : class
            where TTargetProperty : class =>
            (source, target, referenceTracker) =>
            {
                var sourceValue = sourceGetter(source);

                if (sourceValue == null)
                {
                    targetSetter(target, null);
                    return;
                }

                var targetProp = targetConstructor();

                targetSetter(target, targetProp);
                mappingFunc(sourceValue, targetProp, referenceTracker);
            };
        
        private static Action<TSource, TTarget, ReferenceTracker> CreateSimplePropertyMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            PropertyConfig<TSourceProperty, TTargetProperty> cfg)
        {
            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            Action<TSource, TTarget, ReferenceTracker> Func(Func<TSource, TSourceProperty> sourceGetter,
                Action<TTarget, TTargetProperty> propertySetter, Func<TSourceProperty, TTargetProperty> convert) =>
                (source, target, _) => propertySetter(target, convert(sourceGetter(source)));

            return Func(getter, setter, cfg.Converterer.Convert);
        }
    }
}
