using System;
using System.Collections.Generic;
using System.Linq;
using UniCorn.Core;

namespace UniCorn.IoC
{
    public static class ProxyInstanceFactory
    {
        internal static Func<ServiceDescription, object> GetOrCreateProxyInstanceDelegate(Type typeT)
        {
            Func<object[], object> dynamicMethodDelegate = ReflectionUtility.CreateInstanceDelegate(typeT);

            var createInstanceAndInvokeDelegate = new Func<ServiceDescription, object>((ServiceDescription serviceDesc) =>
                {
                    if (serviceDesc.LifeCycle == LifeCycleEnum.Singleton && ((bool)serviceDesc.CreatedInstance?.IsValueCreated))
                        return serviceDesc.CreatedInstance.Value;

                    if (serviceDesc.ConstructorParameters.Length > 0)
                    {
                        bool isProxyType = serviceDesc.ProxyType != null;
                        List<object> parameterValues = new List<object>();

                        foreach (var prm in serviceDesc.ConstructorParameters)
                        {
                            if (isProxyType)
                            {
                                if (prm.ParameterType == typeof(IInterceptor))
                                {
                                    if (serviceDesc.InterceptorType != null)
                                        parameterValues.Add(((ServiceDescription)serviceDesc.InterceptorServiceDesc).ConcreteInstanceCreatorDelegate(null));
                                    else if (serviceDesc.OnInterceptingCallback != null)
                                        parameterValues.Add(new InnerInterceptor(serviceDesc.OnInterceptingCallback));
                                }
                                else if (serviceDesc.OnInstanceCreatingCallback != null)
                                {
                                    parameterValues.Add(serviceDesc.OnInstanceCreatingCallback(serviceDesc.ioCcontainer));
                                }
                                else if (serviceDesc.OnDependenciesCreatingCallback != null)
                                {
                                    IDictionary<string,object> objDependencyDict = serviceDesc.OnDependenciesCreatingCallback(serviceDesc.ioCcontainer).ToExpando();

                                    parameterValues.Add(serviceDesc.ConcreteInstanceCreatorDelegate(objDependencyDict.Values.ToArray()));
                                }
                                else if (serviceDesc.Dependencies != null)
                                {
                                    parameterValues.Add(serviceDesc.ConcreteInstanceCreatorDelegate(serviceDesc.Dependencies.Values.ToArray()));
                                }
                                else
                                {
                                    if (serviceDesc.ConcreteInstanceCreatorDelegate == null)
                                        parameterValues.Add(null);
                                    else
                                        parameterValues.Add(serviceDesc.ConcreteInstanceCreatorDelegate(null));
                                }
                            }
                            else
                            {
                                if (prm.ParameterType == serviceDesc.ServiceType && serviceDesc.OnInstanceCreatingCallback != null)
                                {
                                    parameterValues.Add(serviceDesc.OnInstanceCreatingCallback(serviceDesc.ioCcontainer));
                                }
                                else if (serviceDesc.OnDependenciesCreatingCallback != null)
                                {
                                    IDictionary<string, object> objDependencyDict = serviceDesc.OnDependenciesCreatingCallback(serviceDesc.ioCcontainer).ToExpando();

                                    parameterValues.AddRange(objDependencyDict.Values);
                                }
                                else if (serviceDesc.Dependencies != null)
                                {
                                    if (serviceDesc.Dependencies.TryGetValue(prm.Name, out object prmValue))
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

            return createInstanceAndInvokeDelegate;
        }
    }
}