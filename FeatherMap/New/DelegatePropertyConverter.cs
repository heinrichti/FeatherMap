using System;

namespace FeatherMap.New
{
    internal class DelegatePropertyConverter<TSource, TTarget> : IPropertyConverter<TSource, TTarget>
    {
        private readonly Func<TSource, TTarget> _converFunc;

        public DelegatePropertyConverter(Func<TSource, TTarget> converFunc)
        {
            _converFunc = converFunc;
        }

        public TTarget Convert(TSource source) => _converFunc(source);
    }
}
