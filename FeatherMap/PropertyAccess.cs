using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FeatherMap
{
    internal static class PropertyAccess
    {
        internal static Action<T, TProp> CreateSetter<T, TProp>(PropertyInfo propertyInfo)
        {
            // TODO this is significantly slower on .NET Framework than propertyInfo.GetSetMethod().CreateDelegate(typeof(Action<T, TProp>));
            var instance = Expression.Parameter(typeof(T));
            var argument = Expression.Parameter(typeof(TProp));

            var propertySetMethod = propertyInfo.GetSetMethod();

            var setterCall = Expression.Call(
                instance,
                propertySetMethod,
                argument);

            return (Action<T, TProp>)Expression.Lambda(setterCall, instance, argument).Compile();
        }

        internal static Func<T, TProp> CreateGetter<T, TProp>(PropertyInfo propertyInfo)
        {
            // TODO this is significantly slower on .NET Framework than propertyInfo.GetGetMethod().CreateDelegate(typeof(Func<T, TProp>));
            var instance = Expression.Parameter(typeof(T));
            var property = Expression.Property(instance, propertyInfo);
            return (Func<T, TProp>)Expression.Lambda(property, instance).Compile();
        }
    }
}
