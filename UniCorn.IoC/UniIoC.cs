using System;
using UniCorn.Core;

namespace UniCorn.IoC
{
    public class UniIoC : IUniIoC
    {
        internal AvlTree<object, ServiceDescription> container;

        public UniIoC()
        {
            container = new AvlTree<object, ServiceDescription>();
        }

        public static IUniIoC NewInstance()
        {
            return new UniIoC();
        }

        public void Register(params IServiceDescription[] serviceDescriptions)
        {
            if (serviceDescriptions == null)
                throw new ArgumentNullException("serviceDescriptions");

            ServiceDescription serviceDescription;
            bool canContinue = true;
            RegisterBehaviorEnum registerBhvr;
            foreach (ServiceDescription item in serviceDescriptions)
            {
                registerBhvr = item.RegisterBehavior;

                item.Refresh(this);

                if (registerBhvr == RegisterBehaviorEnum.Keep)
                {
                    canContinue = false;

                    serviceDescription = container.GetValue(item.ServiceKey);

                    if (serviceDescription == null)
                        canContinue = true;
                }
                else if (registerBhvr == RegisterBehaviorEnum.Throw)
                {
                    canContinue = false;

                    throw new InvalidOperationException("Already registered.");
                }

                if (canContinue)
                {
                    container.AddOrUpdate(item.ServiceKey, item);
                }
            }
        }

        public void Register<TService, TImplementation>(Func<IServiceDescription, IServiceDescription> serviceDescriptionFunc = null) where TImplementation : class where TService : class
        {
            IServiceDescription serviceDesc = ServiceCriteria.For<TService>().ImplementedBy<TImplementation>();

            if (serviceDescriptionFunc != null)
                serviceDesc = serviceDescriptionFunc(serviceDesc);

            Register(serviceDesc);
        }

        public void UnRegister(object serviceKey = null)
        {
            container.Delete(serviceKey);
        }

        public void UnRegister<TService>()
        {
            object serviceKey = typeof(TService);

            UnRegister(serviceKey);
        }

        public TService Resolve<TService>(object serviceKey = null) where TService : class
        {
            return (TService)Resolve(serviceKey ?? typeof(TService));
        }

        public object Resolve(object serviceKey)
        {
            ServiceDescription serviceDescription = container.GetValue(serviceKey);

            if (serviceDescription == null)
                throw new NullReferenceException("serviceDescription");

            if (serviceDescription.LifeCycle == LifeCycleEnum.Singleton)
                return serviceDescription.CreatedInstance;

            if (serviceDescription.InterceptorType == null && serviceDescription.InterceptorCallback == null && serviceDescription.InstanceCreatorCallback != null)
                return serviceDescription.InstanceCreatorCallback(this);
            else
                return serviceDescription.NewInstanceCreatorDelegate(serviceDescription);
            //return serviceDescription.ConcreteInstanceCreatorDelegate(null);
        }

        public bool IsRegistered(object serviceKey)
        {
            if (serviceKey == null)
                throw new ArgumentNullException("serviceKey");

            ServiceDescription serviceDescription = container.GetValue(serviceKey);

            return serviceDescription != null;
        }

        public bool IsRegistered<TService>()
        {
            object serviceKey = typeof(TService);

            return IsRegistered(serviceKey);
        }

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

        //~UniIoC()
        //{
        //    Dispose(false);
        //}
    }
}