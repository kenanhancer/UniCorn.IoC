using System;

namespace UniCorn.IoC
{
    public interface IUniIoC : IDisposable
    {
        void Register(params IServiceDescription[] serviceDescriptions);

        void Register<TService, TImplementation>(Func<IServiceDescription, IServiceDescription> serviceDescriptionFunc = null) where TService : class where TImplementation : class, TService ;

        void UnRegister<TService>() where TService : class;

        void UnRegister(object serviceKey);

        TService Resolve<TService>(object name = null) where TService : class;

        object Resolve(object serviceKey);

        bool IsRegistered(object serviceKey);

        bool IsRegistered<TService>() where TService : class;

        IServiceDescription GetServiceDescription(object serviceKey);
    }
}