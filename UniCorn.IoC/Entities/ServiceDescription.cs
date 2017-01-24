using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using UniCorn.Core;

namespace UniCorn.IoC
{
    public static class ServiceDescriptionExtension
    {
        public static IServiceDescription For<TResolve>(this IServiceDescription service) where TResolve : class
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.For<TResolve>();

            return service;
        }

        public static IServiceDescription ImplementedBy<TConcrete>(this IServiceDescription service) where TConcrete : class
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.ImplementedBy<TConcrete>();
            return service;
        }

        public static IServiceDescription ImplementedBy(this IServiceDescription service, Type concreteType)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.ImplementedBy(concreteType);
            return service;
        }

        public static IServiceDescription Named(this IServiceDescription service, object serviceKey)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Named(serviceKey);
            return service;
        }

        public static IServiceDescription Dependencies(this IServiceDescription service, object dependencies)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Arguments(dependencies);
            return service;
        }

        public static IServiceDescription Dependencies(this IServiceDescription service, IDictionary<string, object> dependencies)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Arguments(dependencies);
            return service;
        }

        public static IServiceDescription Instance(this IServiceDescription service, object instance)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Instance(instance);
            return service;
        }

        public static IServiceDescription OnInstanceCreating(this IServiceDescription service, Func<IUniIoC, object> callback)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.OnInstanceCreating(callback);
            return service;
        }

        public static IServiceDescription OnIntercepting(this IServiceDescription service, Action<IInvocation> callback)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.OnIntercepting(callback);
            return service;
        }

        public static IServiceDescription Interceptor<TInterceptor>(this IServiceDescription service) where TInterceptor : IInterceptor
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Interceptor<TInterceptor>();
            return service;
        }

        public static IServiceDescription Interceptor(this IServiceDescription service, Type interceptorType)
        {
            ServiceDescription innerService = service as ServiceDescription;
            innerService.Interceptor(interceptorType);
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

    public interface IServiceDescription
    {
        //string Name { get; }
        //Type ResolveType { get; }
        //Type ConcreteType { get; }
        //Type ConcreteProxyType { get; }
        //Type InterceptorType { get; }
        //IServiceDescription InterceptorServiceDesc { get; }
        //ConstructorInfo Constructor { get; }
        //ParameterInfo[] ConstructorParameters { get; }
        //IDictionary<string, object> ConcreteConstructorArguments { get; }
        //LifeCycleEnum LifeCycle { get; }
        //object CreatedInstance { get; set; }
        //Action<UniCorn.IoC.IInvocation> InterceptorCallback { get; }
        //Func<object[], object> ConcreteInstanceCreatorDelegate { get; }
        //Func<IUniIoC, object> InstanceCreatorCallback { get; }
        //Func<IServiceDescription, object> NewInstanceCreatorDelegate { get; }

        //void Refresh();
    }

    public partial class ServiceCriteria
    {
        public static IServiceDescription For<TService>() where TService : class
        {
            return ServiceDescription.New.For<TService>();
        }

        public static IServiceDescription For(Type serviceType)
        {
            return ServiceDescription.New.For(serviceType);
        }
    }

    internal class ServiceDescription : IServiceDescription
    {
        public object ServiceKey { get; private set; }
        public Type ResolveType { get; private set; }
        public Type ConcreteType { get; private set; }
        public Type ConcreteProxyType { get; private set; }
        public Type InterceptorType { get; private set; }
        public ServiceDescription InterceptorServiceDesc { get; private set; }
        public ConstructorInfo Constructor { get; private set; }
        public ParameterInfo[] ConstructorParameters { get; private set; }
        public IDictionary<string, object> Dependencies { get; private set; }
        public LifeCycleEnum LifeCycle { get; set; }
        public RegisterBehaviorEnum RegisterBehavior { get; set; }
        public object CreatedInstance { get; set; }
        public Action<IInvocation> InterceptorCallback { get; private set; }
        public Func<object[], object> ConcreteInstanceCreatorDelegate { get; private set; }
        public Func<IUniIoC, object> InstanceCreatorCallback { get; private set; }
        public Func<ServiceDescription, object> NewInstanceCreatorDelegate { get; private set; }
        public Func<object[], object> AnonymousInstantiator { get; private set; }
        internal IUniIoC ioCcontainer;

        protected ServiceDescription() { }

        public ServiceDescription(object serviceKey, Type resolveType, Type concreteType, LifeCycleEnum lifeCycle, RegisterBehaviorEnum registerBehavior, Func<IUniIoC, object> instanceCreatorCallback, object dependencies, Type interceptorType = null, Action<IInvocation> interceptorCallback = null)
        {
            ServiceKey = serviceKey;
            ResolveType = resolveType;
            ConcreteType = concreteType;
            LifeCycle = lifeCycle;
            RegisterBehavior = registerBehavior;
            InstanceCreatorCallback = instanceCreatorCallback;
            InterceptorType = interceptorType;
            InterceptorCallback = interceptorCallback;
            if (dependencies != null)
                Dependencies = dependencies.ToExpando();
        }

        public void Refresh(UniIoC ioCcontainer)
        {
            if (ioCcontainer == null)
                throw new ArgumentNullException("ioCcontainer");

            Interlocked.CompareExchange(ref this.ioCcontainer, ioCcontainer, null);

            if (InterceptorType != null || InterceptorCallback != null)
                ConcreteProxyType = ProxyTypeFactory.CreateProxyType(ConcreteType, ResolveType);

            Type innerConcreteType = ConcreteProxyType ?? ConcreteType;

            ServiceKey = ServiceKey ?? ResolveType ?? ConcreteProxyType ?? ConcreteType;

            NewInstanceCreatorDelegate = ProxyInstanceFactory.GetOrCreateProxyInstanceDelegate(innerConcreteType);

            AnonymousInstantiator = GetAnonymousInstantiator(innerConcreteType);

            if (InterceptorType != null)
            {
                List<Type> prmTypes = new List<Type>();
                prmTypes.Add(typeof(IInterceptor));

                if (InstanceCreatorCallback != null)
                {
                    prmTypes.Add(ResolveType ?? innerConcreteType);
                }

                Constructor = innerConcreteType.GetConstructor(prmTypes.ToArray());

                InterceptorServiceDesc = ioCcontainer.container.GetValue(InterceptorType);
            }
            else if (innerConcreteType == null && InstanceCreatorCallback != null)
            {
                ServiceDescription serviceDesc = ioCcontainer.container.GetValue(ResolveType);

                innerConcreteType = serviceDesc.ConcreteProxyType ?? serviceDesc.ConcreteType;
            }

            if (ConcreteType != null && InstanceCreatorCallback == null)
                ConcreteInstanceCreatorDelegate = UniCornTypeFactory.CreateInstanceDelegate(ConcreteType);

            if (innerConcreteType == null)
                return;

            Constructor = innerConcreteType.GetConstructor(Type.EmptyTypes);

            if (Constructor == null)
            {
                Constructor = innerConcreteType.GetConstructors().FirstOrDefault();
            }

            if (Constructor != null)
            {
                ConstructorParameters = Constructor.GetParameters();
            }

            if (LifeCycle == LifeCycleEnum.Singleton)
            {
                if (!IsProxy && InstanceCreatorCallback != null)
                {
                    CreatedInstance = InstanceCreatorCallback(ioCcontainer);
                }
                else
                {
                    CreatedInstance = NewInstanceCreatorDelegate(this);
                }
            }
        }

        bool IsProxy => InterceptorType != null || InterceptorCallback != null;

        public static ServiceDescription New
        {
            get
            {
                return new ServiceDescription();
            }
        }

        public IServiceDescription For<TResolve>() where TResolve : class
        {
            Type resolveType = typeof(TResolve);
            TypeInfo resolveTypeInfo = resolveType.GetTypeInfo();

            if (resolveTypeInfo.IsClass)
                this.ConcreteType = resolveType;
            else if (resolveTypeInfo.IsInterface)
                this.ResolveType = resolveType;

            return this;
        }

        public IServiceDescription For(Type resolveType)
        {
            TypeInfo resolveTypeInfo = resolveType.GetTypeInfo();

            if (resolveTypeInfo.IsClass)
                this.ConcreteType = resolveType;
            else if (resolveTypeInfo.IsInterface)
                this.ResolveType = resolveType;

            return this;
        }

        public IServiceDescription ImplementedBy<TConcrete>() where TConcrete : class
        {
            this.ConcreteType = typeof(TConcrete);
            return this;
        }

        public IServiceDescription ImplementedBy(Type concreteType)
        {
            this.ConcreteType = concreteType;
            return this;
        }

        public IServiceDescription Named(object serviceKey)
        {
            this.ServiceKey = serviceKey;
            return this;
        }

        public IServiceDescription Arguments(object arguments)
        {
            this.Dependencies = arguments.ToExpando();
            return this;
        }

        public IServiceDescription Arguments(IDictionary<string, object> arguments)
        {
            this.Dependencies = arguments;
            return this;
        }

        public IServiceDescription Instance(object instance)
        {
            this.CreatedInstance = instance;
            return this;
        }

        public IServiceDescription OnInstanceCreating(Func<IUniIoC, object> callback)
        {
            this.InstanceCreatorCallback = callback;
            return this;
        }

        public IServiceDescription OnIntercepting(Action<IInvocation> callback)
        {
            this.InterceptorCallback = callback;
            return this;
        }

        public IServiceDescription Interceptor<TInterceptor>()
        {
            this.InterceptorType = typeof(TInterceptor);
            return this;
        }

        public IServiceDescription Interceptor(Type interceptorType)
        {
            this.InterceptorType = interceptorType;
            return this;
        }

        public IServiceDescription SingletonLifeCycle
        {
            get
            {
                this.LifeCycle = LifeCycleEnum.Singleton;
                return this;
            }
        }

        public IServiceDescription TransientLifeCycle
        {
            get
            {
                this.LifeCycle = LifeCycleEnum.Transient;
                return this;
            }
        }

        public static Func<object[], object> GetAnonymousInstantiator(Type type)
        {
            var ctor = type.GetConstructors().First();
            var paramExpr = Expression.Parameter(typeof(object[]));
            return Expression.Lambda<Func<object[], object>>
            (
                Expression.New
                (
                    ctor,
                    ctor.GetParameters().Select
                    (
                        (x, i) => Expression.Convert
                        (
                            Expression.ArrayIndex(paramExpr, Expression.Constant(i)),
                            x.ParameterType
                        )
                    )
                ), paramExpr).Compile();
        }
    }
}