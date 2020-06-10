using System;

namespace FeatherMap
{
    public class PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>
    {
        private IPropertyConverter<TSourceProperty, TTargetProperty> _propertyConverter;
        private Mapping<TSourceProperty, TTargetProperty> _mapping;

        internal Direction Direction { get; private set; } = Direction.TwoWay;

        internal IPropertyConverter<TSourceProperty, TTargetProperty> PropertyConverter
        {
            get
            {
                if (typeof(TSourceProperty) != typeof(TTargetProperty) && _propertyConverter == null)
                {
                    throw new ArgumentException("Different Property-Types and no converter specified");
                }

                if (_propertyConverter == null)
                    return (IPropertyConverter<TSourceProperty, TTargetProperty>)EmptyPropertyConverter<TSourceProperty>.Instance;

                return _propertyConverter;
            }
        }

        internal Mapping<TSourceProperty, TTargetProperty> Mapping => _mapping;

        public PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> MappingDirection(Direction direction)
        {
            Direction = direction;
            return this;
        }

        public PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> UseConverter(IPropertyConverter<TSourceProperty, TTargetProperty> converter)
        {
            if(_mapping != null)
                throw new ArgumentException("Converter cannot be used together with custom mapping.");

            _propertyConverter = converter;
            return this;
        }

        public PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty> UseMapping(Mapping<TSourceProperty, TTargetProperty> mapping)
        {
            if(_propertyConverter != null)
                throw new ArgumentException("Mapping cannot be used together with custom converter");

            _mapping = mapping;
            return this;
        }
    }
}
