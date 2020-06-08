using System;
using System.Linq.Expressions;

namespace FeatherMap
{
    public interface IMappingBuilder<TSource, TTarget>
    {
        IMappingBuilder<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> sourceProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty,
            Func<PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>,
                PropertyConfig<TSource, TTarget, TSourceProperty, TTargetProperty>> cfg);

        IMappingBuilder<TSource, TTarget> Bind<TSourceProperty, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> sourceProperty,
            Expression<Func<TTarget, TTargetProperty>> targetProperty);

        Mapping<TSource, TTarget> Build();
    }
}
