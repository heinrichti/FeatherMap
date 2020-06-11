using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap.New
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

            if (!targetPropertyInfo.CanWrite)
                throw new ArgumentException("Property is not writeable: " + typeof(TSource).Name + "->" + sourceMember.Member.Name);

            return BindInternal(sourcePropertyInfo, targetPropertyInfo, propertyConfig);
        }

        internal MappingConfiguration<TSource, TTarget> BindInternalWithoutConfig<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo
        ) =>
            BindInternal(sourcePropertyInfo, targetPropertyInfo,
                new PropertyConfig<TSourceProperty, TTargetProperty>());

        internal MappingConfiguration<TSource, TTarget> BindInternal<TSourceProperty, TTargetProperty>(
            PropertyInfo sourcePropertyInfo,
            PropertyInfo targetPropertyInfo,
            PropertyConfig<TSourceProperty, TTargetProperty> config
        )
        {
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
    }
}
