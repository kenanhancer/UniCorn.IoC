namespace UniCorn.IoC
{
    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }
}