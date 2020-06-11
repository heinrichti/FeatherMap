using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap
{
    internal static class PropertyAccess
    {
        internal static Action<T, TProp> CreateSetter<T, TProp>(PropertyInfo propertyInfo)
        {
#if NETFULL
            return (Action<T, TProp>)propertyInfo.GetSetMethod().CreateDelegate(typeof(Action<T, TProp>));
#else
            var instance = Expression.Parameter(typeof(T));
            var argument = Expression.Parameter(typeof(TProp));

            var propertySetMethod = propertyInfo.GetSetMethod();

            var setterCall = Expression.Call(
                instance,
                propertySetMethod,
                argument);

            return (Action<T, TProp>) Expression.Lambda(setterCall, instance, argument).Compile();
#endif
        }

        internal static Func<T, TProp> CreateGetter<T, TProp>(PropertyInfo propertyInfo)
        {
#if NETFULL
            return (Func<T, TProp>) propertyInfo.GetGetMethod().CreateDelegate(typeof(Func<T, TProp>));
#else
            var instance = Expression.Parameter(typeof(T));
            var property = Expression.Property(instance, propertyInfo);
            return (Func<T, TProp>)Expression.Lambda(property, instance).Compile();
#endif
        }

        internal static Func<T> GetDefaultConstructor<T>()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda(typeof(Func<T>), newExp);
            return (Func<T>)lambda.Compile();
        }
    }
}
