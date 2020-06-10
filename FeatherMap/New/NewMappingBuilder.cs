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
            where TSource : new()
            where TTarget : new()
        {
            return FullAuto<TSource, TTarget>(typeof(TSource), typeof(TTarget));
        }

        private static Action<TSource, TTarget> FullAuto<TSource, TTarget>(Type sourceType, Type targetType)
            where TSource : new()
            where TTarget : new()
        {
            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);
            var simplePropertyMapMethod = typeof(NewMappingBuilder).GetMethod(nameof(CreateSimplePropertyMap), BindingFlags.Static | BindingFlags.NonPublic);
            var complexPropertyMapMethod = typeof(NewMappingBuilder).GetMethod(nameof(CreateComplexMap), BindingFlags.Static | BindingFlags.NonPublic);

            var actions = new List<Action<TSource, TTarget>>();

            foreach (var sourceProperty in sourceProperties)
            {
                if (targetPropertyDictionary.TryGetValue(sourceProperty.Name, out var targetProperty))
                {
                    if (!sourceProperty.PropertyType.IsAnsiClass)
                    {
                        var createMapFunc = complexPropertyMapMethod.MakeGenericMethod(sourceType, sourceProperty.PropertyType, targetType, targetProperty.PropertyType);
                        var complexPropertyMapping = (Action<TSource, TTarget>) createMapFunc.Invoke(null,
                            new object[] {sourceProperty, targetProperty});
                        actions.Add(complexPropertyMapping);
                    }
                    else
                    {
                        var createMapFunc = simplePropertyMapMethod.MakeGenericMethod(sourceType, sourceProperty.PropertyType, targetType, targetProperty.PropertyType);
                        var converterFuncType = typeof(Func<,>).MakeGenericType(sourceProperty.PropertyType,
                            targetProperty.PropertyType);

                        var mapMethodInfo = typeof(NewMappingBuilder).GetMethod(nameof(Map), BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(sourceProperty.PropertyType, targetProperty.PropertyType);
                        var mappingFunc = mapMethodInfo.CreateDelegate(converterFuncType);

                        var simplePropertyMapping = (Action<TSource, TTarget>) createMapFunc.Invoke(null,
                            new object[] {sourceProperty, targetProperty, mappingFunc});
                        actions.Add(simplePropertyMapping);
                    }
                }
            }

            return actions.Aggregate((accumulate, action) => accumulate + action);
        }

        private static TOut Map<TIn, TOut>(TIn input) where TIn : TOut => input;

        private static Action<TSource, TTarget> CreateComplexMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty)
        {
            var createMapFunc = typeof(NewMappingBuilder).GetMethod(nameof(FullAuto), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(sourceProperty.PropertyType, targetProperty.PropertyType);

            var mapFunc = (Action<TSourceProperty, TTargetProperty>)createMapFunc.Invoke(null, new object[] { sourceProperty.PropertyType, targetProperty.PropertyType });
            var constructor = GetDefaultConstructor<TTargetProperty>();

            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            void MappingFunc(TSource source, TTarget target)
            {
                var targetProp = constructor();
                var sourceProp = getter(source);
                mapFunc(sourceProp, targetProp);
                setter(target, targetProp);
            }

            return MappingFunc;
        }

        private static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }

        private static Action<TSource, TTarget> CreateSimplePropertyMap<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            Func<TSourceProperty, TTargetProperty> converterFunc)
        {
            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            return (source, target) => setter(target, converterFunc(getter(source)));
        }
    }
}
