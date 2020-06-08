using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FeatherMap
{
    public class AutoPropertyConfig<TSource, TTarget>
    {
        internal Dictionary<string, (string TargetPropertyName, object Config)> PropertyConfigs = new Dictionary<string, (string TargetPropertyName, object Config)>();

        internal Direction DirectionInternal { get; private set; } = FeatherMap.Direction.TwoWay;

        public AutoPropertyConfig<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> sourceProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty,
            Func<PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>, PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>> configFunc)
        {
            var config = configFunc(new PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>());
            var sourceMember = (MemberExpression)sourceProperty.Body;
            var targetMember = (MemberExpression)targetProperty.Body;

            PropertyConfigs.Add(sourceMember.Member.Name, (targetMember.Member.Name, config));
            return this;
        }

        public AutoPropertyConfig<TSource, TTarget> Direction(Direction direction)
        {
            DirectionInternal = direction;
            return this;
        }

        internal HashSet<string> PropertiesToIgnore { get; } = new HashSet<string>();

        public AutoPropertyConfig<TSource, TTarget> Ignore<TProperty>(Expression<Func<TSource, TProperty>> property)
        {
            var sourceMember = (MemberExpression)property.Body;
            PropertiesToIgnore.Add(sourceMember.Member.Name);

            return this;
        }
    }
}
