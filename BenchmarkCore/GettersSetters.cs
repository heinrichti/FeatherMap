using System;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace BenchmarkCore
{
    [SimpleJob]
    public class GettersSetters
    {
        private Func<TestClass, string> _getDelegate;
        private Action<TestClass, string> _setDelegate;
        private Func<TestClass, string> _getExpression;
        private Action<TestClass, string> _setExpression;
        private TestClass _testClass;

        private static Action<T, TProp> CreateSetter<T, TProp>(PropertyInfo propertyInfo)
        {
            var instance = Expression.Parameter(typeof(T));
            var argument = Expression.Parameter(typeof(TProp));

            var propertySetMethod = propertyInfo.GetSetMethod();

            var setterCall = Expression.Call(
                instance,
                propertySetMethod,
                argument);

            return (Action<T, TProp>)Expression.Lambda(setterCall, instance, argument).Compile();
        }

        private static Func<T, TProp> CreateGetter<T, TProp>(PropertyInfo propertyInfo)
        {
            var instance = Expression.Parameter(typeof(T));
            var property = Expression.Property(instance, propertyInfo);
            return (Func<T, TProp>)Expression.Lambda(property, instance).Compile();
        }

        [GlobalSetup]
        public void Setup()
        {
            var type = typeof(TestClass);
            var propertyInfo = type.GetProperty(nameof(TestClass.TestProp), 
                BindingFlags.Instance | BindingFlags.Public);

            _getDelegate = (Func<TestClass, string>)propertyInfo.GetMethod.CreateDelegate(typeof(Func<TestClass, string>));
            _setDelegate = (Action<TestClass, string>)propertyInfo.SetMethod.CreateDelegate(typeof(Action<TestClass, string>));

            _getExpression = CreateGetter<TestClass, string>(propertyInfo);
            _setExpression = CreateSetter<TestClass, string>(propertyInfo);

            _testClass = new TestClass();
        }

        [Benchmark]
        public void GetDelegate()
        {
            var tmp = _getDelegate(_testClass);
        }

        [Benchmark]
        public void SetDelegate()
        {
            _setDelegate(_testClass, "Test");
        }

        [Benchmark]
        public void GetExpression()
        {
            var tmp = _getExpression(_testClass);
        }

        [Benchmark]
        public void SetExpression()
        {
            _setExpression(_testClass, "Test");
        }

        private class TestClass
        {
            public string TestProp { get; set; } = "TestInit";
        }
    }
}
