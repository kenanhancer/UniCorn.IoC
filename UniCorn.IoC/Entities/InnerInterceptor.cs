using System;

namespace UniCorn.IoC
{
    public class InnerInterceptor : IInterceptor
    {
        Action<IInvocation> interceptor;

        public InnerInterceptor(Action<IInvocation> interceptor) => this.interceptor = interceptor;

        public void Intercept(IInvocation invocation)
        {
            if (interceptor != null)
                interceptor(invocation);
            else
                invocation.Proceed();
        }
    }
}