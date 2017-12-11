using System.Reflection;

namespace UniCorn.IoC
{
    public interface IInvocation
    {
        MethodInfo Method { get; }

        object[] MethodParameters { get; }

        object ReturnValue { get; set; }

        object Proceed(object newTarget = null);
    }
}