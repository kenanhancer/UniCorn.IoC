using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniCorn.IoC
{
    public class InnerInterceptor : IInterceptor
    {
        Action<UniCorn.IoC.IInvocation> interceptor;

        public InnerInterceptor(Action<UniCorn.IoC.IInvocation> interceptor)
        {
            this.interceptor = interceptor;
        }

        public void Intercept(IInvocation invocation)
        {
            if (interceptor != null)
                interceptor(invocation);
            else
                invocation.Proceed();
        }
    }
}