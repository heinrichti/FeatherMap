using System;
using System.Collections.Generic;

namespace FeatherMap.Configuration
{
    public class PropertyConfig<TSourceProperty, TTargetProperty>
    {
        private readonly Dictionary<SourceToTargetMap, object> _typeConfigs;
        private IPropertyConverter<TSourceProperty, TTargetProperty> _converterer;

        internal PropertyConfig(Dictionary<SourceToTargetMap, object> typeConfigs) => _typeConfigs = typeConfigs;

        internal IPropertyConverter<TSourceProperty, TTargetProperty> Converter
        {
            get => _converterer ?? IdentityPropertyConverter<TSourceProperty>.Instance as IPropertyConverter<TSourceProperty, TTargetProperty>;
            private set => _converterer = value;
        }

        internal MappingConfiguration<TSourceProperty, TTargetProperty> MappingConfiguration { get; private set; }

        public PropertyConfig<TSourceProperty, TTargetProperty> Convert(
            IPropertyConverter<TSourceProperty, TTargetProperty> converter)
        {
            Converter = converter;
            return this;
        }

        public PropertyConfig<TSourceProperty, TTargetProperty> Convert(
            Func<TSourceProperty, TTargetProperty> convertFunc)
        {
            Converter = new DelegatePropertyConverter<TSourceProperty, TTargetProperty>(convertFunc);
            return this;
        }

        public PropertyConfig<TSourceProperty, TTargetProperty> CreateMap(
            Func<MappingConfiguration<TSourceProperty, TTargetProperty>, MappingConfiguration<TSourceProperty, TTargetProperty>> cfgAction)
        {
            MappingConfiguration = cfgAction(new MappingConfiguration<TSourceProperty, TTargetProperty>(_typeConfigs));
            return this;
        }

        public PropertyConfig<TSourceProperty, TTargetProperty> Auto()
            => Auto(cfg => cfg);

        public PropertyConfig<TSourceProperty, TTargetProperty> Auto(
            Func<MappingConfiguration<TSourceProperty, TTargetProperty>, MappingConfiguration<TSourceProperty, TTargetProperty>> cfgAction)
        {
            var mappingConfiguration = new MappingConfiguration<TSourceProperty, TTargetProperty>(_typeConfigs);
            mappingConfiguration = cfgAction(mappingConfiguration);

            MappingConfiguration = ConfigurationBuilder.Auto(_typeConfigs,
                mappingConfiguration);
            return this;
        }
    }
}
