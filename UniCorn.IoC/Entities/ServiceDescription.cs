using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using UniCorn.BaseLibrary;
using UniCorn.Core;

namespace UniCorn.IoC
{
    internal class ServiceDescription : IServiceDescription
    {
        #region Fields

        public object ServiceKey { get; set; }
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public Type ProxyType { get; set; }
        public Type InterceptorType { get; set; }
        public IServiceDescription InterceptorServiceDesc { get; set; }
        public ConstructorInfo Constructor { get; set; }
        public ParameterInfo[] ConstructorParameters { get; set; }
        public IDictionary<string, object> Dependencies { get; set; }
        public LifeCycleEnum LifeCycle { get; set; }
        public RegisterBehaviorEnum RegisterBehavior { get; set; }
        public Lazy<object> CreatedInstance { get; set; }
        public Action<IInvocation> OnInterceptingCallback { get; set; }
        public Func<object[], object> ConcreteInstanceCreatorDelegate { get; set; }
        public Func<IUniIoC, object> OnInstanceCreatingCallback { get; set; }
        public Func<IUniIoC, object> OnDependenciesCreatingCallback { get; set; }
        public Func<ServiceDescription, object> NewInstanceCreatorDelegate { get; set; }
        public Func<object[], object> AnonymousInstantiator { get; set; }
        public LambdaExpression AnonymousInstantiatorLambda { get; set; }
        public bool IsProxy => InterceptorType != null || OnInterceptingCallback != null;
        internal IUniIoC ioCcontainer;

        #endregion Fields

        #region Constructors
        protected ServiceDescription() { }

        public ServiceDescription(object serviceKey, Type serviceType, Type implementationType, LifeCycleEnum lifeCycle, RegisterBehaviorEnum registerBehavior, Func<IUniIoC, object> onInstanceCreatingCallback, Func<IUniIoC, object> onDependenciesCreatingCallback, Type interceptorType = null, Action<IInvocation> onInterceptingCallback = null)
        {
            ServiceKey = serviceKey;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            LifeCycle = lifeCycle;
            RegisterBehavior = registerBehavior;
            OnInstanceCreatingCallback = onInstanceCreatingCallback;
            OnDependenciesCreatingCallback = onDependenciesCreatingCallback;
            InterceptorType = interceptorType;
            OnInterceptingCallback = onInterceptingCallback;

            //if (dependencies != null)
            //    Dependencies = dependencies.ToExpando();
        }

        public ServiceDescription(object serviceKey, Type serviceType, Type implementationType, LifeCycleEnum lifeCycle, RegisterBehaviorEnum registerBehavior, Func<IUniIoC, object> onInstanceCreatingCallback, object dependencies, Type interceptorType = null, Action<IInvocation> onInterceptingCallback = null)
        {
            ServiceKey = serviceKey;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            LifeCycle = lifeCycle;
            RegisterBehavior = registerBehavior;
            OnInstanceCreatingCallback = onInstanceCreatingCallback;
            //OnDependenciesCreatingCallback = onDependenciesCreatingCallback;
            InterceptorType = interceptorType;
            OnInterceptingCallback = onInterceptingCallback;

            if (dependencies != null)
                Dependencies = dependencies.ToExpando();
        }

        #endregion Constructors

        #region ServiceDescription Builder

        public IServiceDescription For<TService>() where TService : class => For(typeof(TService));

        public IServiceDescription For(Type serviceType)
        {
            this.ServiceType = serviceType;
            return this;
        }

        #endregion ServiceDescription Builder

        public static ServiceDescription New => new ServiceDescription();

        public void Refresh(IUniIoC ioCcontainer)
        {
            if (ioCcontainer == null)
                throw new ArgumentNullException("ioCcontainer");

            Interlocked.CompareExchange(ref this.ioCcontainer, ioCcontainer, null);

            ImplementationType = ImplementationType ?? ServiceType;

            if (IsProxy)
            {
                ProxyType = ProxyTypeUtility.CreateProxyType(ServiceType ?? ImplementationType);
            }

            Type innerConcreteType = ProxyType ?? ImplementationType;

            ServiceKey = ServiceKey ?? ServiceType ?? ProxyType ?? ImplementationType;

            if (BaseTypeConstants.LambdaExpressionTypeInfo.IsAssignableFrom(innerConcreteType))
                AnonymousInstantiatorLambda = ServiceType.CreateAnonymousInstantiatorLambda();
            else if (BaseTypeConstants.GenericDelegateTypeInfo.IsAssignableFrom(innerConcreteType))
                AnonymousInstantiator = ServiceType.CreateAnonymousInstantiator();
            else
            {
                if (ImplementationType != null && OnInstanceCreatingCallback == null && !ImplementationType.IsInterface)
                    ConcreteInstanceCreatorDelegate = ReflectionUtility.CreateInstanceDelegate(ImplementationType);

                if (InterceptorType != null)
                {
                    List<Type> prmTypes = new List<Type>();
                    prmTypes.Add(typeof(IInterceptor));

                    if (OnInstanceCreatingCallback != null)
                    {
                        prmTypes.Add(ServiceType ?? innerConcreteType);
                    }

                    Constructor = innerConcreteType.GetConstructor(prmTypes.ToArray());

                    //InterceptorServiceDesc = ioCcontainer.container.GetValue(InterceptorType);
                    InterceptorServiceDesc = ioCcontainer.GetServiceDescription(InterceptorType);
                }
                else if (innerConcreteType == null && OnInstanceCreatingCallback != null)
                {
                    //ServiceDescription serviceDesc = ioCcontainer.container.GetValue(ServiceType);
                    ServiceDescription serviceDesc = ioCcontainer.GetServiceDescription(ServiceType) as ServiceDescription;

                    innerConcreteType = serviceDesc.ProxyType ?? serviceDesc.ImplementationType;
                }

                if (innerConcreteType == null)
                    return;

                if (innerConcreteType.IsClass)
                {
                    NewInstanceCreatorDelegate = ProxyInstanceFactory.GetOrCreateProxyInstanceDelegate(innerConcreteType);

                    Constructor = innerConcreteType.GetConstructor(Type.EmptyTypes);

                    if (Constructor == null)
                    {
                        Constructor = innerConcreteType.GetConstructors().FirstOrDefault();
                    }

                    if (Constructor != null)
                    {
                        ConstructorParameters = Constructor.GetParameters();
                    }

                }

                if (LifeCycle == LifeCycleEnum.Singleton)
                {
                    if (!IsProxy && OnInstanceCreatingCallback != null)
                    {
                        CreatedInstance = new Lazy<object>(() => OnInstanceCreatingCallback(ioCcontainer), LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                    else
                    {
                        CreatedInstance = new Lazy<object>(() => NewInstanceCreatorDelegate(this), LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                }
            }
        }
    }

    public interface IServiceDescription
    {
    }

    public static class ServiceDescriptionExtension
    {
        public static IServiceDescription For<TService>(this IServiceDescription service) where TService : class
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.For<TService>();
            return service;
        }

        public static IServiceDescription ImplementedBy<TImplementation>(this IServiceDescription service) where TImplementation : class
        {
            return service.ImplementedBy(typeof(TImplementation));
        }

        public static IServiceDescription ImplementedBy(this IServiceDescription service, Type implementationType)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.ImplementationType = implementationType;
            return service;
        }

        public static IServiceDescription Named(this IServiceDescription service, object serviceKey)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.ServiceKey = serviceKey;
            return service;
        }

        public static IServiceDescription Dependencies(this IServiceDescription service, object dependencies)
        {
            return service.Dependencies((IDictionary<string, object>)dependencies.ToExpando());
        }

        public static IServiceDescription Dependencies(this IServiceDescription service, IDictionary<string, object> dependencies)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Dependencies = dependencies;
            return service;
        }

        public static IServiceDescription Instance(this IServiceDescription service, object instance)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.CreatedInstance = new Lazy<object>(() => instance);
            return service;
        }

        public static IServiceDescription OnInstanceCreating(this IServiceDescription service, Func<IUniIoC, object> callback)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.OnInstanceCreatingCallback = callback;
            return service;
        }

        public static IServiceDescription OnDependenciesCreating(this IServiceDescription service, Func<IUniIoC, object> callback)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.OnDependenciesCreatingCallback = callback;
            return service;
        }

        public static IServiceDescription OnIntercepting(this IServiceDescription service, Action<IInvocation> callback)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.OnInterceptingCallback = callback;
            return service;
        }

        public static IServiceDescription Interceptor<TInterceptor>(this IServiceDescription service) where TInterceptor : class, IInterceptor
        {
            return service.Interceptor(typeof(TInterceptor));
        }

        public static IServiceDescription Interceptor(this IServiceDescription service, Type interceptorType)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.InterceptorType = interceptorType;
            return service;
        }

        public static IServiceDescription LifeCycle(this IServiceDescription service, LifeCycleEnum value)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.LifeCycle = value;
            return service;
        }

        public static IServiceDescription RegisterBehavior(this IServiceDescription service, RegisterBehaviorEnum value)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.RegisterBehavior = value;
            return service;
        }
    }

    public partial class ServiceCriteria
    {
        public static IServiceDescription For<TService>() where TService : class => ServiceDescription.New.For<TService>();

        public static IServiceDescription For(Type serviceType) => ServiceDescription.New.For(serviceType);
    }
}