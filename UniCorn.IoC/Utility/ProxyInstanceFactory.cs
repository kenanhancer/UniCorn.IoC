using System;
using System.Collections.Generic;
using System.Linq;
using UniCorn.Core;

namespace UniCorn.IoC
{
    public static class ProxyInstanceFactory
    {
        static AvlTree<object, Func<ServiceDescription, object>> proxyInstanceDelegateList;

        static ProxyInstanceFactory()
        {
            proxyInstanceDelegateList = new AvlTree<object, Func<ServiceDescription, object>>();
        }

        internal static Func<ServiceDescription, object> GetOrCreateProxyInstanceDelegate(Type typeT)
        {
            Func<ServiceDescription, object> createInstanceAndInvokeDelegate = proxyInstanceDelegateList.GetValue(typeT);

            if (createInstanceAndInvokeDelegate == null)
            {
                Func<object[], object> dynamicMethodDelegate = UniCornTypeFactory.CreateInstanceDelegate(typeT);

                createInstanceAndInvokeDelegate = new Func<ServiceDescription, object>((ServiceDescription serviceDesc) =>
                {
                    if (serviceDesc.LifeCycle == LifeCycleEnum.Singleton && serviceDesc.CreatedInstance != null)
                        return serviceDesc.CreatedInstance;

                    if (serviceDesc.ConstructorParameters.Length > 0)
                    {
                        object prmValue;
                        bool isProxyType = serviceDesc.ConcreteProxyType != null;
                        List<object> parameterValues = new List<object>();

                        foreach (var prm in serviceDesc.ConstructorParameters)
                        {
                            if (isProxyType)
                            {
                                if (prm.ParameterType == typeof(IInterceptor))
                                {
                                    if (serviceDesc.InterceptorType != null)
                                        parameterValues.Add(serviceDesc.InterceptorServiceDesc.ConcreteInstanceCreatorDelegate(null));
                                    else if (serviceDesc.InterceptorCallback != null)
                                        parameterValues.Add(new InnerInterceptor(serviceDesc.InterceptorCallback));
                                }
                                else if (serviceDesc.InstanceCreatorCallback != null)
                                {
                                    parameterValues.Add(serviceDesc.InstanceCreatorCallback(serviceDesc.ioCcontainer));
                                }
                                else if (serviceDesc.Dependencies != null)
                                {
                                    parameterValues.Add(serviceDesc.ConcreteInstanceCreatorDelegate(serviceDesc.Dependencies.Values.ToArray()));
                                }
                                else
                                {
                                    parameterValues.Add(serviceDesc.ConcreteInstanceCreatorDelegate(null));
                                }
                            }
                            else
                            {
                                if (prm.ParameterType == serviceDesc.ResolveType && serviceDesc.InstanceCreatorCallback != null)
                                {
                                    parameterValues.Add(serviceDesc.InstanceCreatorCallback(serviceDesc.ioCcontainer));
                                }
                                else if (serviceDesc.Dependencies != null)
                                {
                                    if (serviceDesc.Dependencies.TryGetValue(prm.Name, out prmValue))
                                        parameterValues.Add(prmValue);
                                }
                                else
                                {
                                    parameterValues.Add(serviceDesc.ioCcontainer.Resolve(prm.ParameterType));
                                }
                            }
                        }

                        return dynamicMethodDelegate(parameterValues.ToArray());
                    }
                    return dynamicMethodDelegate(null);
                });

                proxyInstanceDelegateList.AddOrUpdate(typeT, createInstanceAndInvokeDelegate);
            }

            return createInstanceAndInvokeDelegate;
        }
    }
}