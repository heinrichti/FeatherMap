using System.Reflection;

namespace FeatherMap.New
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
        internal override object ConfigObject => Config;
    }
}
