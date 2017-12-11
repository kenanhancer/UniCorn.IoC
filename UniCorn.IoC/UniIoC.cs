using System;
using System.Collections.Concurrent;

namespace UniCorn.IoC
{
    public class UniIoC : IUniIoC
    {
        internal ConcurrentDictionary<object, ServiceDescription> container = new ConcurrentDictionary<object, ServiceDescription>();

        public static IUniIoC NewInstance() => new UniIoC();

        public void Register(params IServiceDescription[] serviceDescriptions)
        {
            if (serviceDescriptions == null)
                throw new ArgumentNullException("serviceDescriptions");

            ServiceDescription serviceDescription;
            RegisterBehaviorEnum registerBhvr;
            foreach (ServiceDescription item in serviceDescriptions)
            {
                registerBhvr = item.RegisterBehavior;

                if (registerBhvr == RegisterBehaviorEnum.Keep || registerBhvr == RegisterBehaviorEnum.AddOrUpdate)
                {
                    item.Refresh(this);

                    serviceDescription = container.GetOrAdd(item.ServiceKey, item);
                }
                else if (registerBhvr == RegisterBehaviorEnum.Throw)
                {
                    throw new InvalidOperationException("Already registered.");
                }
            }
        }

        public void Register<TService, TImplementation>(Func<IServiceDescription, IServiceDescription> serviceDescriptionFunc = null) where TService : class where TImplementation : class, TService
        {
            IServiceDescription serviceDesc = ServiceCriteria.For<TService>().ImplementedBy<TImplementation>();

            if (serviceDescriptionFunc != null)
                serviceDesc = serviceDescriptionFunc(serviceDesc);

            Register(serviceDesc);
        }

        public void UnRegister(object serviceKey = null) => container.TryRemove(serviceKey, out ServiceDescription srvDesc);

        public void UnRegister<TService>() where TService : class => UnRegister(typeof(TService));

        public TService Resolve<TService>(object serviceKey = null) where TService : class
        {
            Type tService = typeof(TService);

            serviceKey = serviceKey ?? tService;

            if (!container.TryGetValue(serviceKey, out ServiceDescription serviceDescription) || serviceDescription == null)
            {
                Type serviceType = serviceKey as Type;

                if (serviceType.IsClass)
                {
                    Register(ServiceCriteria.For(serviceType).ImplementedBy(tService));

                    if (!container.TryGetValue(serviceKey, out serviceDescription))
                        throw new NullReferenceException("serviceDescription");
                }
            }

            if (serviceDescription.AnonymousInstantiator != null)
                return serviceDescription.AnonymousInstantiator as TService;
            else if (serviceDescription.AnonymousInstantiatorLambda != null)
                return serviceDescription.AnonymousInstantiatorLambda as TService;

            if (serviceDescription.LifeCycle == LifeCycleEnum.Singleton)
                return serviceDescription.CreatedInstance.Value as TService;

            if (serviceDescription.InterceptorType == null && serviceDescription.OnInterceptingCallback == null && serviceDescription.OnInstanceCreatingCallback != null)
                return serviceDescription.OnInstanceCreatingCallback(this) as TService;
            else
                return serviceDescription.NewInstanceCreatorDelegate(serviceDescription) as TService;
        }

        public object Resolve(object serviceKey) => Resolve<object>(serviceKey);

        public bool IsRegistered(object serviceKey)
        {
            if (serviceKey == null)
                throw new ArgumentNullException("serviceKey");

            return container.ContainsKey(serviceKey);
        }

        public bool IsRegistered<TService>() where TService : class => IsRegistered(typeof(TService));

        public void Dispose()
        {
            Dispose(true);

            //GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                //m_connection.Close();
                //m_connection.Dispose();
            }

            container = null;
        }

        public IServiceDescription GetServiceDescription(object serviceKey)
        {
            container.TryGetValue(serviceKey, out ServiceDescription serviceDesc);
            return serviceDesc;
        }

        //~UniIoC()
        //{
        //    Dispose(false);
        //}
    }
}