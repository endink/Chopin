using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Chopin.Pooling.Proxy
{
    internal static class DynamicAssembly
    {
        static DynamicAssembly()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Chopin.Pooling.Proxy.DynamicAssembly"),
                AssemblyBuilderAccess.Run);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule("mian");
        }
        internal static readonly ModuleBuilder ModuleBuilder;
    }

    internal class DefaultProxyCreater<T> : ProxyCreator<T>
    {
        private const string FieldName = "<youcan'tdefinenamelikethis_hahaha!>";
        private static readonly Func<object, DefaultProxyHandler> _fieldReader;
        private static readonly Type ProxyType;
        private static Func<object, DefaultProxyHandler> BuildFieldReader()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(DefaultProxyHandler), new[] { typeof(object) }, ProxyType, true);
            var info = ProxyType.GetTypeInfo().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null) throw new Exception("An unknown error occurred!");
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, info);
            il.Emit(OpCodes.Ret);
            return null;
        }

        static DefaultProxyCreater()
        {

        }



        public T CreateProxy(T pooledObject, IUsageTracking<T> tracker)
        {
            throw new NotImplementedException();
        }

        public T ResolveProxy(T proxy)
        {
            return default(T);
            //var payload = proxy as IPayload<T>;
            //return payload.Handler.DisableProxy();
        }

        private sealed class DefaultProxyHandler : BaseProxyHandler<T>
        {
            public DefaultProxyHandler(T pooledObject, IUsageTracking<T> tracker)
                : base(pooledObject, tracker)
            {
            }
            private static ConcurrentDictionary<MethodInfo, Func<object, object[], object>> Cache
                = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();
            internal override object Invoke(MethodInfo method, object[] args)
            {
                ValidateProxiedObject();
                var @object = PooledObject;
                Tracker?.Use(@object);
                if (method == null) throw new ArgumentNullException(nameof(method));
                return Cache.GetOrAdd(method, meth =>
                {
                    var p1 = Expression.Parameter(typeof(object));
                    var p2 = Expression.Parameter(typeof(object[]));
                    var type = meth.DeclaringType;
                    if (type == null) throw new NotSupportedException("not supported the Type with no DeclaringType");
                    var instance = Expression.Convert(p1, type);
                    var call = Expression.Call(instance, meth, meth.GetParameters()
                        .Select((item, index) => Expression.Convert(
                            Expression.ArrayIndex(p2, Expression.Constant(index)), item.ParameterType)));
                    if (meth.ReturnType != typeof(void))
                        return Expression
                            .Lambda<Func<object, object[], object>>(Expression.Convert(call, typeof(object)), p1, p2)
                            .Compile();
                    var block = Expression.Block(call, instance);
                    return Expression.Lambda<Func<object, object[], object>>(block, p1, p2).Compile();
                })(@object, args);
            }
        }
    }
}
