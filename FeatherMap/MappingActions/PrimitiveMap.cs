using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FeatherMap.Configuration;

namespace FeatherMap.MappingActions
{
    internal static class PrimitiveMap
    {
        internal static Action<TSource, TTarget, ReferenceTracker> Create<TSource, TSourceProperty, TTarget, TTargetProperty>(
            PropertyInfo sourceProperty, 
            PropertyInfo targetProperty,
            PropertyConfig<TSourceProperty, TTargetProperty> cfg)
        {
            var getter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var setter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            Action<TSource, TTarget, ReferenceTracker> Func(Func<TSource, TSourceProperty> sourceGetter,
                Action<TTarget, TTargetProperty> propertySetter, Func<TSourceProperty, TTargetProperty> convert) =>
                (source, target, _) => propertySetter(target, convert(sourceGetter(source)));

            return Func(getter, setter, cfg.Converter.Convert);
        }
    }
}
