using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FeatherMap.MappingActions
{
    internal static class ListMap
    {
        internal static Action<TSource, TTarget, ReferenceTracker> Create<TSource, TTarget, TSourceProperty,
            TTargetProperty, TSourceInstanceType, TTargetInstanceType>(PropertyInfo sourceProperty,
            PropertyInfo targetProperty, 
            Dictionary<SourceToTargetMap, MapTracker> existingMaps)
            where TSourceProperty : class, IList<TSourceInstanceType>
            where TTargetProperty : class, IList<TTargetInstanceType>
        {
            var sourceInstanceType = typeof(TSourceInstanceType);
            var sourceGetter = PropertyAccess.CreateGetter<TSource, TSourceProperty>(sourceProperty);
            var targetSetter = PropertyAccess.CreateSetter<TTarget, TTargetProperty>(targetProperty);

            Func<TSourceInstanceType, TTargetInstanceType> convertFunc;

            if (sourceInstanceType.IsPrimitive || sourceInstanceType == typeof(string) ||
                sourceInstanceType.IsValueType)
            {
                var converter = IdentityPropertyConverter<TSourceInstanceType>.Instance as IPropertyConverter<TSourceInstanceType, TTargetInstanceType>;
                convertFunc = converter.Convert;
            }
            else
            {
                var mapping = Mapping<TSourceInstanceType, TTargetInstanceType>.Auto();
                convertFunc = mapping.Clone;
            }

            void Map(TSource source, TTarget target, ReferenceTracker tracker)
            {
                var sourceList = sourceGetter(source);
                var result = new List<TTargetInstanceType>(sourceList.Count);

                for (int i = 0; i < sourceList.Count; i++)
                {
                    var item = sourceList[i];
                    result.Add(convertFunc(item));
                }

                targetSetter(target, result as TTargetProperty);
            }

            return Map;
        }
    }
}
