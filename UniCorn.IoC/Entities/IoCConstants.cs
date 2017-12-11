using System;
using System.Collections.Generic;
using System.Reflection;
using UniCorn.BaseLibrary;

namespace UniCorn.IoC
{
    public static class IoCConstants
    {
        public static readonly Type interceptorType = typeof(IInterceptor);
        public static readonly Type invocationType = typeof(IInvocation);
        public static readonly Type proxyMethodsType = typeof(Dictionary<string, ProxyMethod>);
        public static readonly Type proxyMethodType = typeof(ProxyMethod);
        public static readonly Type proxyInterfaceType = typeof(IProxyType);
        public static readonly MethodInfo interceptMi = interceptorType.GetMethod("Intercept");
        public static readonly MethodInfo dictTryGetValueMi = proxyMethodsType.GetMethod("TryGetValue");
        public static readonly MethodInfo getProxyMethodsMi = typeof(ProxyTypeUtility).GetMethod("CreateMethodInvokers", BindingFlags.Public | BindingFlags.Static);
        public static readonly MethodInfo invocationTypeReturnValueMi = IoCConstants.invocationType.GetProperty("ReturnValue").GetGetMethod();

        public static readonly ConstructorInfo standartInvocationCi = typeof(StandardInvocation).GetConstructor(new Type[] { typeof(object[]), BaseTypeConstants.ObjectType, typeof(ProxyMethod) });
    }
}