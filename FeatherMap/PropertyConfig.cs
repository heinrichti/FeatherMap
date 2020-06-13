using System;

namespace FeatherMap
{
    public class PropertyConfig<TSourceProperty, TTargetProperty>
    {
        private IPropertyConverter<TSourceProperty, TTargetProperty> _converterer;
        
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
            MappingConfiguration = cfgAction(new MappingConfiguration<TSourceProperty, TTargetProperty>());
            return this;
        }
    }
}
