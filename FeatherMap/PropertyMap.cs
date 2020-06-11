using System.Reflection;

namespace FeatherMap
{
    internal abstract class PropertyMapBase
    {
        internal PropertyMapBase(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo)
        {
            SourcePropertyInfo = sourcePropertyInfo;
            TargetPropertyInfo = targetPropertyInfo;
        }

        internal abstract bool HasMappingConfiguration();

        internal abstract object GetMappingConfiguration();

        internal PropertyInfo TargetPropertyInfo { get; }

        internal PropertyInfo SourcePropertyInfo { get; }

        internal abstract object ConfigObject { get; }
    }

    internal class PropertyMap<TSourceProperty, TTargetProperty> : PropertyMapBase
    {
        public PropertyConfig<TSourceProperty, TTargetProperty> Config { get; }

        public PropertyMap(PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo,
            PropertyConfig<TSourceProperty, TTargetProperty> config)
            : base(sourcePropertyInfo, targetPropertyInfo) =>
            Config = config;

        internal override bool HasMappingConfiguration() => Config.MappingConfiguration != null;

        internal override object GetMappingConfiguration() => Config.MappingConfiguration;

        internal override object ConfigObject => Config;
    }
}
