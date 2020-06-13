using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FeatherMap.Configuration
{
    internal static class ConfigurationBuilder
    {
        internal  static MappingConfiguration<TSource, TTarget> Auto<TSource, TTarget>(
            Dictionary<SourceToTargetMap, object> typeConfigs, 
            MappingConfiguration<TSource, TTarget> mappingConfiguration)
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            if (typeConfigs.TryGetValue(new SourceToTargetMap(sourceType, targetType), out var previousMappingConfig))
                return (MappingConfiguration<TSource, TTarget>) previousMappingConfig;

            typeConfigs.Add(new SourceToTargetMap(sourceType, targetType), mappingConfiguration);

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetPropertyDictionary = targetProperties.ToDictionary(x => x.Name, x => x);

            foreach (var sourceProperty in sourceProperties)
            {
                if (mappingConfiguration.PropertiesToIgnore.Contains(sourceProperty.Name))
                    continue;

                if (mappingConfiguration.PropertyMaps.Any(x => x.SourcePropertyInfo == sourceProperty))
                    continue;

                if (targetPropertyDictionary.TryGetValue(sourceProperty.Name, out var targetProperty))
                {
                    if (!targetProperty.CanWrite)
                        continue;

                    var bindFunc = typeof(ConfigurationBuilder).GetMethod(nameof(BindComplexConfig),
                            BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(typeof(TSource), typeof(TTarget), sourceProperty.PropertyType, targetProperty.PropertyType);

                    bindFunc.Invoke(null, new object[] {sourceProperty, targetProperty, mappingConfiguration, typeConfigs});
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
                new PropertyConfig<TSourceProperty, TTargetProperty>(typeMappings)
                    .CreateMap(x => Auto(typeMappings,
                        mappingConfiguration.GetChildConfigOrNew<TSourceProperty, TTargetProperty>(
                            sourceProperty, targetProperty))));
        }
    }
}
