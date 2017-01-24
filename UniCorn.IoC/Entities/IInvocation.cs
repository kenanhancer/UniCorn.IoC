using System.Reflection;

namespace UniCorn.IoC
{
    public interface IInvocation
    {
        MethodInfo Method { get; }

        object[] MethodParameters { get; }

        object ReturnValue { get; set; }

        void Proceed();
    }
}