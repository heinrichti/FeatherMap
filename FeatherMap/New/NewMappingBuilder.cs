using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap.New
{
    public class NewMappingBuilder
    {
        public static Action<TSource, TTarget> Auto<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var mapResult = FullAuto<TSource, TTarget>(sourceType, targetType,
                new Dictionary<SourceToTargetMap, Delegate>());
            if (mapResult.RequiresReferenceTracking.Any())
                return (source, target) =>
                {
                    var referenceTracker = new ReferenceTracker();
                    referenceTracker.Add(new TargetTypeSourceObject(targetType, source), target);
                    mapResult.MappingFunc(source, target, referenceTracker);
                };

            Func<Action<TSource, TTarget, ReferenceTracker>, Action<TSource, TTarget>> f =
                mappingAction => (s1, s2) => mappingAction(s1, s2, null);

            return f(mapResult.MappingFunc);
        }

        private static ComplexMapResult<TSource, TTarget> FullAuto<TSource, TTarget>(
            Type sourceType, 
            Type targetType,
            Dictionary<SourceToTargetMap, Delegate> typeMappings)
        {
            if (typeMappings.TryGetValue(
                new SourceToTargetMap(sourceType, targetType),
                out var func))
            {
                void Test(TSource source, TTarget target, ReferenceTracker referenceTracker)
                {
                    var value = (Func<Delegate>) func;
                    var action = (Action<TSource, TTarget, ReferenceTracker>) value();
                    action(source, target, referenceTracker);
                }

                return new ComplexMapResult<TSource, TTarget>(Test,
                    new List<ReferenceTrackingRequired>
                        {new ReferenceTrackingRequired(typeof(TSource), typeof(TTarget))});
            }

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);
            var simplePropertyMapMethod = typeof(NewMappingBuilder).GetMethod(nameof(CreateSimplePropertyMap), BindingFlags.Static | BindingFlags.NonPublic);
            var complexPropertyMapMethod = typeof(NewMappingBuilder).GetMethod(nameof(CreateComplexMap), BindingFlags.Static | BindingFlags.NonPublic);

            Action<TSource, TTarget, ReferenceTracker> result = (source, target, referenceTracker) => { };
            Delegate ResultingMap() => result;
            typeMappings.Add(new SourceToTargetMap(sourceType, targetType, ResultingMap), (Func<Delegate>) ResultingMap);

            var requiresReferenceCheck = new List<ReferenceTrackingRequired>();
            var actions = new List<Action<TSource, TTarget, ReferenceTracker>>();

            foreach (var sourceProperty in sourceProperties)
            {
                if (targetPropertyDictionary.TryGetValue(sourceProperty.Name, out var targetProperty))
                {
                    if (!targetProperty.CanWrite)
                        continue;

                    if (sourceProperty.PropertyType.IsPrimitive || sourceProperty.PropertyType == typeof(string) || sourceProperty.PropertyType.IsValueType)
                    {
                        var createMapFunc = simplePropertyMapMethod.MakeGenericMethod(sourceType,
                            sourceProperty.PropertyType, targetType, targetProperty.PropertyType);

                        var simplePropertyMapping = (Action<TSource, TTarget, ReferenceTracker>) createMapFunc.Invoke(
                            null,
                            new object[] {sourceProperty, targetProperty});
                        actions.Add(simplePropertyMapping);
                    }
                    else
                    {
                        var createMapFunc = complexPropertyMapMethod.MakeGenericMethod(sourceType,
                            sourceProperty.PropertyType, targetType, targetProperty.PropertyType);
                        var complexPropertyMapping = (ComplexMapResult<TSource, TTarget>) createMapFunc.Invoke(null,
                            new object[] {sourceProperty, targetProperty, typeMappings});
                        requiresReferenceCheck.AddRange(complexPropertyMapping.RequiresReferenceTracking);
                        actions.Add(complexPropertyMapping.MappingFunc);
                    }
                }
            }

            result = actions.Aggregate((accumulate, action) => accumulate + action);
            return new ComplexMapResult<TSource, TTarget>(result, requiresReferenceCheck);
        }

        private class ComplexMapResult<T, TU>
        {
            public ComplexMapResult(Action<T, TU, ReferenceTracker> mappingFunc, List<ReferenceTrackingRequired> requiresReferenceTracking)
            {
                MappingFunc = mappingFunc;
                RequiresReferenceTracking = requiresReferenceTracking;
            }

            public Action<T, TU, ReferenceTracker> MappingFunc { get; }

            public List<ReferenceTrackingRequired> RequiresReferenceTracking { get; }
        }

        private static ComplexMapResult<TSource, TTarget> CreateComplexMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            Dictionary<SourceToTargetMap, Delegate> typeMappings)
        {
            var createMapFunc = typeof(NewMappingBuilder).GetMethod(nameof(FullAuto), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(sourceProperty.PropertyType, targetProperty.PropertyType);

            var mapFuncResult = (ComplexMapResult<TSourceProperty, TTargetProperty>)createMapFunc
                .Invoke(null, new object[] { sourceProperty.PropertyType, targetProperty.PropertyType, typeMappings });
            var mapFunc = mapFuncResult.MappingFunc;
            var constructor = GetDefaultConstructor<TTargetProperty>();

            var sourcePropertyGetter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            var requiresReferenceTracking = mapFuncResult.RequiresReferenceTracking;

            Action<TSource, TTarget, ReferenceTracker> MappingFuncWithReferenceTracking(
                Func<TSource, TSourceProperty> sourceGetter,
                Action<TTarget, TTargetProperty> targetSetter,
                Func<TTargetProperty> targetConstructor,
                Action<TSourceProperty, TTargetProperty, ReferenceTracker> mappingFunc) =>
                (source, target, referenceTracker) =>
                {
                    var sourceValue = sourceGetter(source);

                    if (sourceValue == null)
                    {
                        targetSetter(target, default);
                        return;
                    }

                    var sourceTargetType = new TargetTypeSourceObject(typeof(TTargetProperty), sourceValue);

                    object alreadyMappedObject = null;
                    if (referenceTracker?.TryGet(sourceTargetType, out alreadyMappedObject) ?? false)
                    {
                        targetSetter(target, (TTargetProperty) alreadyMappedObject);
                        return;
                    }

                    var targetProp = targetConstructor();

                    //referenceTracker?.Add(sourceTargetType, targetProp);

                    targetSetter(target, targetProp);
                    mappingFunc(sourceValue, targetProp, referenceTracker);
                };

            Action<TSource, TTarget, ReferenceTracker> MappingWithoutReferenceTracking(
                Func<TSource, TSourceProperty> sourceGetter, 
                Action<TTarget, TTargetProperty> targetSetter, 
                Func<TTargetProperty> targetConstructor, 
                Action<TSourceProperty, TTargetProperty, ReferenceTracker> mappingFunc) =>
                (source, target, referenceTracker) =>
                {
                    var sourceValue = sourceGetter(source);

                    if (sourceValue == null)
                    {
                        targetSetter(target, default);
                        return;
                    }

                    var targetProp = targetConstructor();

                    targetSetter(target, targetProp);
                    mappingFunc(sourceValue, targetProp, referenceTracker);
                };

            if (requiresReferenceTracking.Any(x => 
                x.Source == sourceProperty.PropertyType && x.Target == targetProperty.PropertyType))
                return new ComplexMapResult<TSource, TTarget>(MappingFuncWithReferenceTracking(sourcePropertyGetter, setter, constructor, mapFunc), 
                    requiresReferenceTracking);

            return new ComplexMapResult<TSource, TTarget>(
                MappingWithoutReferenceTracking(sourcePropertyGetter, setter, constructor, mapFunc), 
                requiresReferenceTracking);
        }

        private static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }

        private static Action<TSource, TTarget, ReferenceTracker> CreateSimplePropertyMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty)
        {
            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            if (typeof(TSourceProperty) != typeof(TTargetProperty))
            {
                throw new ArgumentException("Different Property-Types and no converter specified");
            }

            var identityPropertyConverter = IdentityPropertyConverter<TSourceProperty>.Instance as IPropertyConverter<TSourceProperty, TTargetProperty>;

            Action<TSource, TTarget, ReferenceTracker> Func(Func<TSource, TSourceProperty> sourceGetter,
                Action<TTarget, TTargetProperty> propertySetter, Func<TSourceProperty, TTargetProperty> convert) =>
                (source, target, _) => propertySetter(target, convert(sourceGetter(source)));

            return Func(getter, setter, identityPropertyConverter.Convert);
        }
    }
}
