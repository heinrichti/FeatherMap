using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap.Configuration
{
    public class MappingConfiguration<TSource, TTarget>
    {
        private readonly Dictionary<SourceToTargetMap, object> _typeConfigs;
        internal List<PropertyMapBase> PropertyMaps { get; } = new List<PropertyMapBase>();

        internal bool ReferenceTrackingEnabled { get; private set; } = true;

        //public MappingConfiguration<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
        //    Expression<Func<TSource, List<TSourceProperty>>> sourceProperty,
        //    Expression<Func<TTarget, List<TTargetProperty>>> targetProperty,
        //    Action<PropertyConfig<TSourceProperty, TTargetProperty>> config)
        //{
        //    var sourceMember = (MemberExpression)sourceProperty.Body;
        //    var targetMember = (MemberExpression)targetProperty.Body;

        //    var sourcePropertyInfo = sourceMember.Member.DeclaringType.GetProperty(sourceMember.Member.Name);
        //    var targetPropertyInfo = targetMember.Member.DeclaringType.GetProperty(targetMember.Member.Name);

        //    //this.BindInternal(sourcePropertyInfo, targetPropertyInfo, config);

        //    return this;
        //}

        public MappingConfiguration<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> sourceProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty) =>
            Bind(sourceProperty, targetProperty, config => { });

        public MappingConfiguration<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> sourceProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty,
            Action<PropertyConfig<TSourceProperty, TTargetProperty>> configAction)
        {
            var sourceMember = (MemberExpression)sourceProperty.Body;
            var targetMember = (MemberExpression)targetProperty.Body;

            var sourcePropertyInfo = sourceMember.Member.DeclaringType.GetProperty(sourceMember.Member.Name);
            var targetPropertyInfo = targetMember.Member.DeclaringType.GetProperty(targetMember.Member.Name);

            var propertyConfig = new PropertyConfig<TSourceProperty, TTargetProperty>(_typeConfigs);
            configAction(propertyConfig);

            if (typeof(TSourceProperty) != typeof(TTargetProperty) && propertyConfig.Converter == null)
            {
                throw new ArgumentException("Missing conversion: " + 
                                            sourcePropertyInfo.PropertyType.Name + " --> " + targetPropertyInfo.PropertyType.Name);
            }

            if (!targetPropertyInfo.CanWrite)
                throw new ArgumentException("Property is not writeable: " + typeof(TSource).Name + "->" + sourceMember.Member.Name);

            return BindInternal(sourcePropertyInfo, targetPropertyInfo, propertyConfig);
        }

        internal MappingConfiguration(Dictionary<SourceToTargetMap, object> typeConfigs) => _typeConfigs = typeConfigs;

        internal MappingConfiguration<TSource, TTarget> BindInternal<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo,
            PropertyConfig<TSourceProperty, TTargetProperty> config)
        {
            PropertyMapBase.PropertyType type = PropertyMapBase.PropertyType.Primitive;
            if (sourcePropertyInfo.PropertyType.IsValueType && !sourcePropertyInfo.PropertyType.IsPrimitive && sourcePropertyInfo.PropertyType != typeof(Guid))
                type = PropertyMapBase.PropertyType.Struct;
            else if (sourcePropertyInfo.PropertyType.IsArray)
                type = PropertyMapBase.PropertyType.Array;
            else if (typeof(IList).IsAssignableFrom(sourcePropertyInfo.PropertyType))
                type = PropertyMapBase.PropertyType.List;
            else if (typeof(ICollection).IsAssignableFrom(sourcePropertyInfo.PropertyType))
                type = PropertyMapBase.PropertyType.Collection;
            else if (sourcePropertyInfo.PropertyType.IsClass && sourcePropertyInfo.PropertyType != typeof(string))
                type = PropertyMapBase.PropertyType.Complex;

            var propertyMap = new PropertyMap<TSourceProperty, TTargetProperty>(
                sourcePropertyInfo,
                targetPropertyInfo,
                config,
                type);
            
            PropertyMaps.Add(propertyMap);
            return this;
        }

        public MappingConfiguration<TSource, TTarget> DisableReferenceTracking()
        {
            ReferenceTrackingEnabled = false;
            return this;
        }

        public MappingConfiguration<TSourceProperty, TTargetProperty> GetChildConfigOrNew<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo, PropertyInfo targetPropertyInfo)
        {
            foreach (var propertyMap in PropertyMaps.Where(x => x.SourcePropertyInfo == sourcePropertyInfo && x.TargetPropertyInfo == targetPropertyInfo))
            {
                if (propertyMap.HasMappingConfiguration())
                    return (MappingConfiguration<TSourceProperty, TTargetProperty>) propertyMap.GetMappingConfiguration();
            }

            return new MappingConfiguration<TSourceProperty, TTargetProperty>(_typeConfigs);
        }

        internal HashSet<string> PropertiesToIgnore { get; } = new HashSet<string>();

        public MappingConfiguration<TSource, TTarget> Ignore<TProperty>(Expression<Func<TSource, TProperty>> property)
        {
            var sourceMember = (MemberExpression)property.Body;
            PropertiesToIgnore.Add(sourceMember.Member.Name);
            return this;
        }
    }
}
