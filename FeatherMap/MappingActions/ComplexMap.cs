using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FeatherMap.Configuration;

namespace FeatherMap.MappingActions
{
    internal static class ComplexMap
    {
        internal static ComplexMapResult<TSource, TTarget> Create<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            Dictionary<SourceToTargetMap, MapTracker> typeMappings,
            PropertyConfig<TSourceProperty, TTargetProperty> config)
            where TSource : class
            where TTarget : class
            where TSourceProperty : class
            where TTargetProperty : class
        {
            var createMapFunc = typeof(MappingBuilder).GetMethod(nameof(MappingBuilder.CreateMap), BindingFlags.Static | BindingFlags.NonPublic)
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
    }
}
