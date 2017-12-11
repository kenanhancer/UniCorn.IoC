using System.Reflection;

namespace UniCorn.IoC
{
    public class StandardInvocation : IInvocation
    {
        private object targetObject;
        private MethodInfo method;
        private object[] parameters;
        private object returnValue;
        private ProxyMethod proxyMethod;

        public StandardInvocation(object[] parameters, object targetObject, ProxyMethod proxyMethod)
        {
            this.method = proxyMethod.Method;
            this.parameters = parameters;
            this.targetObject = targetObject;
            this.proxyMethod = proxyMethod;
        }

        public MethodInfo Method => method;

        public object[] MethodParameters => parameters;

        public object ReturnValue
        {
            get => returnValue;
            set => returnValue = value;
        }

        public object Proceed(object newTarget = null)
        {
            object[] parameterArray = new object[parameters.Length + 1];
            parameterArray[0] = newTarget ?? targetObject;
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterArray[i + 1] = parameters[i];
            }

            returnValue = proxyMethod.MethodInvokerDelegate(parameterArray);

            return returnValue;
        }
    }
}