using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap
{
    public class MappingConfiguration<TSource, TTarget>
    {
        internal List<PropertyMapBase> PropertyMaps { get; } = new List<PropertyMapBase>();

        internal bool ReferenceTrackingEnabled { get; private set; } = true;

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

            var propertyConfig = new PropertyConfig<TSourceProperty, TTargetProperty>();
            configAction(propertyConfig);

            if (typeof(TSourceProperty) != typeof(TTargetProperty) && propertyConfig.Converterer == null)
            {
                throw new ArgumentException("Missing conversion: " + 
                                            sourcePropertyInfo.PropertyType.Name + " --> " + targetPropertyInfo.PropertyType.Name);
            }

            if (!targetPropertyInfo.CanWrite)
                throw new ArgumentException("Property is not writeable: " + typeof(TSource).Name + "->" + sourceMember.Member.Name);

            return BindInternal(sourcePropertyInfo, targetPropertyInfo, propertyConfig);
        }

        internal MappingConfiguration<TSource, TTarget> BindInternalWithoutConfig<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo
        )
        {
            if (PropertyMaps.Any(b =>
                b.SourcePropertyInfo == sourcePropertyInfo))
                return this;

            return BindInternal(sourcePropertyInfo, targetPropertyInfo,
                new PropertyConfig<TSourceProperty, TTargetProperty>());
        }

        internal MappingConfiguration<TSource, TTarget> BindInternal<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo,
            PropertyConfig<TSourceProperty, TTargetProperty> config
        )
        {
            var alreadyConfiguredMap = PropertyMaps.FirstOrDefault(x =>
                x.SourcePropertyInfo == sourcePropertyInfo && x.TargetPropertyInfo == targetPropertyInfo);
            if (alreadyConfiguredMap != null)
            {
                Trace.WriteLine("Mapping already configured: " + sourcePropertyInfo.DeclaringType.Name + "." + sourcePropertyInfo.PropertyType.Name +
                                " --> " + targetPropertyInfo.DeclaringType.Name + "." + targetPropertyInfo.PropertyType.Name);
                return this;
            }

            var propertyMap = new PropertyMap<TSourceProperty, TTargetProperty>(
                sourcePropertyInfo,
                targetPropertyInfo,
                config);
            
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

            return new MappingConfiguration<TSourceProperty, TTargetProperty>();
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
