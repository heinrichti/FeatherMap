using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap.New
{
    public class NewMappingBuilder
    {
        public static Action<TSource, TTarget> CreateMap<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var mapResult = FullAuto<TSource, TTarget>(sourceType, targetType,
                new Dictionary<SourceToTargetMap, Delegate>());
            if (mapResult.RequiresReferenceTracking)
                return (source, target) =>
                {
                    var referenceTracker = new ReferenceTracker();
                    referenceTracker.Add(new SourceTargetType(sourceType, targetType), source, target);
                    mapResult.MappingFunc(source, target, referenceTracker);
                };

            return (source, target) => mapResult.MappingFunc(source, target, null);
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

                return new ComplexMapResult<TSource, TTarget>(Test, true);
            }

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);
            var simplePropertyMapMethod = typeof(NewMappingBuilder).GetMethod(nameof(CreateSimplePropertyMap), BindingFlags.Static | BindingFlags.NonPublic);
            var complexPropertyMapMethod = typeof(NewMappingBuilder).GetMethod(nameof(CreateComplexMap), BindingFlags.Static | BindingFlags.NonPublic);

            Action<TSource, TTarget, ReferenceTracker> result = (source, target, referenceTracker) => { };
            Delegate ResultingMap() => result;
            typeMappings.Add(new SourceToTargetMap(sourceType, targetType, ResultingMap), (Func<Delegate>) ResultingMap);

            var requiresReferenceCheck = false;
            var actions = new List<Action<TSource, TTarget, ReferenceTracker>>();

            foreach (var sourceProperty in sourceProperties)
            {
                if (targetPropertyDictionary.TryGetValue(sourceProperty.Name, out var targetProperty))
                {
                    if (!targetProperty.CanWrite)
                        continue;

                    if (!sourceProperty.PropertyType.IsPrimitive && !(sourceProperty.PropertyType == typeof(string)))
                    {
                        var createMapFunc = complexPropertyMapMethod.MakeGenericMethod(sourceType, sourceProperty.PropertyType, targetType, targetProperty.PropertyType);
                        var complexPropertyMapping = (ComplexMapResult<TSource, TTarget>) createMapFunc.Invoke(null,
                            new object[] {sourceProperty, targetProperty, typeMappings});
                        requiresReferenceCheck |= complexPropertyMapping.RequiresReferenceTracking;
                        actions.Add(complexPropertyMapping.MappingFunc);
                    }
                    else
                    {
                        var createMapFunc = simplePropertyMapMethod.MakeGenericMethod(sourceType, sourceProperty.PropertyType, targetType, targetProperty.PropertyType);
                        var converterFuncType = typeof(Func<,>).MakeGenericType(sourceProperty.PropertyType,
                            targetProperty.PropertyType);

                        var mapMethodInfo = typeof(NewMappingBuilder).GetMethod(nameof(Map), BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(sourceProperty.PropertyType, targetProperty.PropertyType);
                        var mappingFunc = mapMethodInfo.CreateDelegate(converterFuncType);

                        var simplePropertyMapping = (Action<TSource, TTarget, ReferenceTracker>) createMapFunc.Invoke(null,
                            new object[] {sourceProperty, targetProperty, mappingFunc});
                        actions.Add(simplePropertyMapping);
                    }
                }
            }

            result = actions.Aggregate((accumulate, action) => accumulate + action);
            return new ComplexMapResult<TSource, TTarget>(result, requiresReferenceCheck);
        }

        private static TOut Map<TIn, TOut>(TIn input) where TIn : TOut => input;

        private class ComplexMapResult<T, TU>
        {
            public ComplexMapResult(Action<T, TU, ReferenceTracker> mappingFunc, bool requiresReferenceTracking)
            {
                MappingFunc = mappingFunc;
                RequiresReferenceTracking = requiresReferenceTracking;
            }

            public Action<T, TU, ReferenceTracker> MappingFunc { get; }

            public bool RequiresReferenceTracking { get; }
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

            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            var requiresReferenceTracking = mapFuncResult.RequiresReferenceTracking;

            void MappingFuncWithReferenceTracking(TSource source, TTarget target, ReferenceTracker referenceTracker)
            {
                var sourceValue = getter(source);

                if (sourceValue == null)
                {
                    setter(target, default);
                    return;
                }

                var sourceTargetType = new SourceTargetType(typeof(TSourceProperty), typeof(TTargetProperty));

                object alreadyMappedObject = null;
                if (!(referenceTracker?.TryGet(
                          sourceTargetType, 
                          sourceValue, 
                          out alreadyMappedObject) ?? true))
                {
                    setter(target, (TTargetProperty) alreadyMappedObject);
                    return;
                }

                var targetProp = constructor();
                referenceTracker?.Add(sourceTargetType, sourceValue, targetProp);

                setter(target, targetProp);
                mapFunc(sourceValue, targetProp, referenceTracker);
            }

            void MappingFuncWithoutReferenceTracking(TSource source, TTarget target, ReferenceTracker referenceTracker)
            {
                var sourceValue = getter(source);

                if (sourceValue == null)
                {
                    setter(target, default);
                    return;
                }

                var targetProp = constructor();
                setter(target, targetProp);
                mapFunc(sourceValue, targetProp, referenceTracker);
            }

            if (requiresReferenceTracking)
                return new ComplexMapResult<TSource, TTarget>(MappingFuncWithReferenceTracking, true);

            return new ComplexMapResult<TSource, TTarget>(MappingFuncWithoutReferenceTracking, false);
        }

        private static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }

        private static Action<TSource, TTarget, ReferenceTracker> CreateSimplePropertyMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            Func<TSourceProperty, TTargetProperty> converterFunc)
        {
            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            return (source, target, referenceTracker) => setter(target, converterFunc(getter(source)));
        }
    }
}
