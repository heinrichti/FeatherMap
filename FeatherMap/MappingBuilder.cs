using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FeatherMap.Configuration;
using FeatherMap.MappingActions;

namespace FeatherMap
{
    internal class MappingBuilder
    {
        public static Action<TSource, TTarget> Create<TSource, TTarget>(
            Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgAction)
        {
            var mappingConfiguration = new MappingConfiguration<TSource, TTarget>(new Dictionary<SourceToTargetMap, object>());
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

        internal static ComplexMapResult<TSource, TTarget> CreateMap<TSource, TTarget>(
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

            var simplePropertyMapMethod = typeof(PrimitiveMap).GetMethod(nameof(PrimitiveMap.Create), BindingFlags.Static | BindingFlags.NonPublic);
            var complexPropertyMapMethod = typeof(ComplexMap).GetMethod(nameof(ComplexMap.Create), BindingFlags.Static | BindingFlags.NonPublic);

            existingMaps.Add(new SourceToTargetMap(sourceType, targetType), referenceTrackingMapping);

            var referenceTrackingTypes = new List<ReferenceTrackingType>();
            var actions = new List<Action<TSource, TTarget, ReferenceTracker>>();

            foreach (var propertyMap in config.PropertyMaps)
            {
                Action<TSource, TTarget, ReferenceTracker> map = null;
                if (propertyMap.Type == PropertyMapBase.PropertyType.Primitive)
                {
                    var mapFuncCreator = simplePropertyMapMethod.MakeGenericMethod(
                        sourceType, propertyMap.SourcePropertyInfo.PropertyType,
                        targetType, propertyMap.TargetPropertyInfo.PropertyType);
                    map = (Action<TSource, TTarget, ReferenceTracker>)mapFuncCreator.Invoke(null, new[]
                        {propertyMap.SourcePropertyInfo, propertyMap.TargetPropertyInfo, propertyMap.ConfigObject});
                    
                }
                else if (propertyMap.Type == PropertyMapBase.PropertyType.Complex)
                    map = CreateComplexMappingAction<TSource, TTarget>(
                        sourceType, 
                        targetType, 
                        existingMaps, 
                        complexPropertyMapMethod, 
                        propertyMap, 
                        referenceTrackingTypes);
                else if (propertyMap.Type == PropertyMapBase.PropertyType.List)
                    map = CreateListMapAction<TSource, TTarget>(existingMaps, propertyMap);

                if (map != null)
                    actions.Add(map);
            }

            var result = actions.Aggregate((accumulate, action) => accumulate + action);
            referenceTrackingMapping.Action = result;
            return new ComplexMapResult<TSource, TTarget>(result, referenceTrackingTypes);
        }

        private static Action<TSource, TTarget, ReferenceTracker> CreateListMapAction<TSource, TTarget>(Dictionary<SourceToTargetMap, MapTracker> existingMaps, PropertyMapBase propertyMap)
        {
            var sourceProperty = propertyMap.SourcePropertyInfo;
            var targetProperty = propertyMap.TargetPropertyInfo;

            var genericArguments = new Type[4 + sourceProperty.PropertyType.GenericTypeArguments.Length +
                                            targetProperty.PropertyType.GenericTypeArguments.Length];
            genericArguments[0] = typeof(TSource);
            genericArguments[1] = typeof(TTarget);
            genericArguments[2] = sourceProperty.PropertyType;
            genericArguments[3] = targetProperty.PropertyType;
            for (int i = 0; i < sourceProperty.PropertyType.GenericTypeArguments.Length; i++)
            {
                genericArguments[i + 4] = sourceProperty.PropertyType.GenericTypeArguments[i];
            }

            for (int i = 0; i < targetProperty.PropertyType.GenericTypeArguments.Length; i++)
            {
                genericArguments[i + 4 + sourceProperty.PropertyType.GenericTypeArguments.Length] =
                    targetProperty.PropertyType.GenericTypeArguments[i];
            }

            var bindFunc = typeof(ListMap).GetMethod(nameof(ListMap.Create),
                    BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(genericArguments);

            return (Action<TSource, TTarget, ReferenceTracker>) bindFunc.Invoke(null,
                    new object[] {sourceProperty, targetProperty, existingMaps});
        }

        private static Action<TSource, TTarget, ReferenceTracker> CreateComplexMappingAction<TSource, TTarget>(Type sourceType, Type targetType,
            Dictionary<SourceToTargetMap, MapTracker> existingMaps, MethodInfo complexPropertyMapMethod, PropertyMapBase propertyMap,
            List<ReferenceTrackingType> referenceTrackingTypes)
        {
            var createMapFunc = complexPropertyMapMethod.MakeGenericMethod(
                sourceType, propertyMap.SourcePropertyInfo.PropertyType,
                targetType, propertyMap.TargetPropertyInfo.PropertyType);
            var complexPropertyMapping = (ComplexMapResult<TSource, TTarget>) createMapFunc.Invoke(null,
                new[] {propertyMap.SourcePropertyInfo, propertyMap.TargetPropertyInfo, existingMaps, propertyMap.ConfigObject});
            referenceTrackingTypes.AddRange(complexPropertyMapping.ReferenceTrackingTypes);
            return complexPropertyMapping.MappingFunc;
        }

        public static Action<TSource, TTarget> Auto<TSource, TTarget>(Func<MappingConfiguration<TSource, TTarget>, MappingConfiguration<TSource, TTarget>> cfgFunc)
        {
            var existingConfigs = new Dictionary<SourceToTargetMap, object>();

            var cfg = new MappingConfiguration<TSource, TTarget>(existingConfigs);
            cfg = cfgFunc(cfg);

            return Create<TSource, TTarget>(configuration =>
                ConfigurationBuilder.Auto(existingConfigs, cfg));
        }
    }
}
