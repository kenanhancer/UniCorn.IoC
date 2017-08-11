using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace UniCorn.Core.UnitTests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper output;

        public UnitTest1(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test1()
        {
            MethodInfo mi = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });

            Delegate writeLine = mi.CreateDelegate();

            writeLine.DynamicInvoke("Hello world");

            //output.WriteLine("Hello world");
        }

        [Fact]
        public void Test2()
        {
            MethodInfo mi = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });

            Delegate writeLine = mi.CreateDelegateV2();

            writeLine.DynamicInvoke("Hello world");
        }

        [Fact]
        public void Test3()
        {
            MethodInfo mi = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });

            Action<string> writeLine = mi.CreateDelegateV3<Action<string>>();

            writeLine("Hello world");
        }
    }
}