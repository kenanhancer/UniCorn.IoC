using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UniCorn.Core;

namespace UniCorn.IoC
{
    public static class ProxyTypeFactory
    {
        static IMemoryCache memoryCache;
        static ProxyTypeFactory()
        {
            memoryCache = new MemoryCache(new MemoryCacheOptions());
        }
        public static Type CreateProxyType(Type serviceType, Type interfaceType)
        {
            string name = $"{serviceType.FullName}_ProxyType";

            Type proxyType = memoryCache.Get(name) as Type;

            if (proxyType == null)
            {
                Type implementType = interfaceType ?? serviceType;

                TypeInfo serviceTypeInfo = serviceType.GetTypeInfo();

                MethodInfo[] serviceMethods = implementType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);


                string typeName = $"{serviceType.Name}_Proxy";

                AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = asmBuilder.DefineDynamicModule("DynamicModule");
                TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

                typeBuilder.AddInterfaceImplementation(IoCConstants.proxyInterfaceType);

                foreach (CustomAttributeData attr in serviceTypeInfo.CustomAttributes)
                {
                    typeBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(attr.Constructor,
                                                   attr.ConstructorArguments.Select(f => f.Value).ToArray(),
                                                   attr.NamedArguments.Select(f => f.TypedValue.ArgumentType.GetProperty(f.MemberName)).ToArray(),
                                                   attr.NamedArguments.Select(f => f.TypedValue.Value).ToArray()));
                }

                if (interfaceType != null)
                    typeBuilder.AddInterfaceImplementation(interfaceType);

                //ConstructorBuilder firstConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { interceptorType });

                ConstructorBuilder secondConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { IoCConstants.interceptorType, implementType });

                ConstructorBuilder staticConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

                List<FieldBuilder> proxyMethodVariableFieldBuilderList = new List<FieldBuilder>();
                Label equityLabel;
                MethodInfo mi;

                #region Static Constructor

                ILGenerator staticConstructorIlGen = staticConstructorBuilder.GetILGenerator();

                staticConstructorIlGen.Emit(OpCodes.Ldtoken, implementType);
                staticConstructorIlGen.Emit(OpCodes.Call, UniCornConstants.GetTypeFromHandleMi);
                staticConstructorIlGen.Emit(OpCodes.Call, IoCConstants.getProxyMethodsMi);

                LocalBuilder proxyMethodsTypeLocalBuilder = staticConstructorIlGen.DeclareLocal(IoCConstants.proxyMethodsType);
                staticConstructorIlGen.Emit(OpCodes.Stloc, proxyMethodsTypeLocalBuilder);

                FieldBuilder proxyMethodVariableFieldBuilder;
                LocalBuilder objVariableLocalBuilder = staticConstructorIlGen.DeclareLocal(UniCornConstants.ObjectType);

                string dictKey;
                for (int i = 0; i < serviceMethods.Length; i++)
                {
                    mi = serviceMethods[i];

                    dictKey = $"{mi.Name}_{mi.GetHashCode()}";

                    proxyMethodVariableFieldBuilder = typeBuilder.DefineField(dictKey, IoCConstants.proxyMethodType, FieldAttributes.Public | FieldAttributes.Static);
                    proxyMethodVariableFieldBuilderList.Add(proxyMethodVariableFieldBuilder);

                    staticConstructorIlGen.Emit(OpCodes.Ldloc, proxyMethodsTypeLocalBuilder);
                    staticConstructorIlGen.Emit(OpCodes.Ldstr, dictKey);
                    staticConstructorIlGen.Emit(OpCodes.Ldsflda, proxyMethodVariableFieldBuilder);
                    staticConstructorIlGen.Emit(OpCodes.Callvirt, IoCConstants.dictTryGetValueMi);

                    staticConstructorIlGen.Emit(OpCodes.Pop);
                }

                staticConstructorIlGen.Emit(OpCodes.Ret);

                #endregion Static Constructor

                #region Constructors

                #region First Constructor

                //ConstructorInfo serviceCi = serviceType.GetConstructor(Type.EmptyTypes) ?? serviceType.GetConstructors().FirstOrDefault();

                //ILGenerator constructorIlGen = firstConstructorBuilder.GetILGenerator();

                //constructorIlGen.Emit(OpCodes.Ldarg_0);
                //constructorIlGen.Emit(OpCodes.Newobj, serviceCi);
                //constructorIlGen.Emit(OpCodes.Stfld, serviceInstanceFieldBuilder);

                //constructorIlGen.Emit(OpCodes.Ldarg_0);
                //constructorIlGen.Emit(OpCodes.Ldarg_1);
                //constructorIlGen.Emit(OpCodes.Stfld, interceptorFieldBuilder);

                //constructorIlGen.Emit(OpCodes.Ret);

                #endregion First Constructor

                #region Second Constructor

                ILGenerator constructorSecondILGen = secondConstructorBuilder.GetILGenerator();

                constructorSecondILGen.Emit(OpCodes.Ldarg_0);
                constructorSecondILGen.Emit(OpCodes.Call, UniCornConstants.ObjectCi);

                constructorSecondILGen.Emit(OpCodes.Ldarg_0);
                constructorSecondILGen.Emit(OpCodes.Ldarg_1);
                FieldBuilder interceptorFieldBuilder = typeBuilder.DefineField("interceptor", IoCConstants.interceptorType, FieldAttributes.Private);
                constructorSecondILGen.Emit(OpCodes.Stfld, interceptorFieldBuilder);

                constructorSecondILGen.Emit(OpCodes.Ldarg_0);
                constructorSecondILGen.Emit(OpCodes.Ldarg_2);
                FieldBuilder serviceInstanceFieldBuilder = typeBuilder.DefineField("service", implementType, FieldAttributes.Private);
                constructorSecondILGen.Emit(OpCodes.Stfld, serviceInstanceFieldBuilder);

                constructorSecondILGen.Emit(OpCodes.Ret);

                #endregion Second Constructor

                #endregion Constructors

                #region Methods

                MethodBuilder methodBuilder;
                ILGenerator methodIlGen;
                ParameterInfo[] parameters;
                LocalBuilder invocationLocalBuilder;
                LocalBuilder paramArrayLocalBuilder;

                for (int i = 0; i < serviceMethods.Length; i++)
                {
                    mi = serviceMethods[i];

                    parameters = mi.GetParameters();

                    methodBuilder = typeBuilder.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Virtual, mi.ReturnType, parameters.Select(f => f.ParameterType).ToArray());

                    //foreach (CustomAttributeData attr in mi.CustomAttributes)
                    //{
                    //    methodBuilder.SetCustomAttribute(
                    //        new CustomAttributeBuilder(attr.Constructor,
                    //                                   attr.ConstructorArguments.Select(f => f.Value).ToArray(),
                    //                                   attr.NamedArguments.Select(f => (PropertyInfo)f.MemberInfo).ToArray(),
                    //                                   attr.NamedArguments.Select(f => f.TypedValue.Value).ToArray()));
                    //}

                    if (mi.IsGenericMethod)
                    {
                        Type[] genericArguments = mi.GetGenericArguments();
                        string[] genericParameterNames = genericArguments.Select(f => f.Name).ToArray();

                        GenericTypeParameterBuilder[] genericParameters = methodBuilder.DefineGenericParameters(genericParameterNames);

                        foreach (GenericTypeParameterBuilder genericParameter in genericParameters)
                        {
                            foreach (Type item in genericArguments)
                            {
                                genericParameter.SetGenericParameterAttributes(item.GetTypeInfo().GenericParameterAttributes);
                            }
                        }
                    }

                    methodIlGen = methodBuilder.GetILGenerator();

                    methodIlGen.Emit(OpCodes.Ldarg_0);
                    methodIlGen.Emit(OpCodes.Ldfld, interceptorFieldBuilder);
                    methodIlGen.Emit(OpCodes.Ldnull);
                    methodIlGen.Emit(OpCodes.Cgt_Un);
                    equityLabel = methodIlGen.DefineLabel();
                    methodIlGen.Emit(OpCodes.Brfalse, equityLabel);

                    methodIlGen.Emit(OpCodes.Nop);

                    //methodIlGen.EmitWriteLine(mi.Name);

                    methodIlGen.Emit(OpCodes.Ldc_I4, parameters.Length);
                    methodIlGen.Emit(OpCodes.Newarr, UniCornConstants.ObjectType);
                    paramArrayLocalBuilder = methodIlGen.DeclareLocal(typeof(object[]));
                    methodIlGen.Emit(OpCodes.Stloc, paramArrayLocalBuilder);

                    for (int a = 0; a < parameters.Length; a++)
                    {
                        methodIlGen.Emit(OpCodes.Ldloc, paramArrayLocalBuilder);
                        methodIlGen.Emit(OpCodes.Ldc_I4, a);
                        methodIlGen.Emit(OpCodes.Ldarg, a + 1);
                        methodIlGen.Emit(OpCodes.Stelem_Ref);
                    }

                    methodIlGen.Emit(OpCodes.Ldloc, paramArrayLocalBuilder);
                    methodIlGen.Emit(OpCodes.Ldarg_0);
                    methodIlGen.Emit(OpCodes.Ldfld, serviceInstanceFieldBuilder);
                    methodIlGen.Emit(OpCodes.Ldsfld, proxyMethodVariableFieldBuilderList[i]);

                    methodIlGen.Emit(OpCodes.Newobj, IoCConstants.standartInvocationCi);
                    invocationLocalBuilder = methodIlGen.DeclareLocal(IoCConstants.invocationType);
                    methodIlGen.Emit(OpCodes.Stloc, invocationLocalBuilder);

                    methodIlGen.Emit(OpCodes.Ldarg_0);
                    methodIlGen.Emit(OpCodes.Ldfld, interceptorFieldBuilder);
                    methodIlGen.Emit(OpCodes.Ldloc, invocationLocalBuilder);
                    methodIlGen.Emit(OpCodes.Callvirt, IoCConstants.interceptMi);


                    methodIlGen.Emit(OpCodes.Ldloc, invocationLocalBuilder);
                    methodIlGen.Emit(OpCodes.Callvirt, IoCConstants.invocationTypeReturnValueMi);
                    methodIlGen.Emit(OpCodes.Unbox_Any, mi.ReturnType);
                    methodIlGen.Emit(OpCodes.Ret);

                    methodIlGen.Emit(OpCodes.Nop);

                    methodIlGen.MarkLabel(equityLabel);

                    #region this.service.Login("","");

                    methodIlGen.Emit(OpCodes.Ldarg_0);
                    methodIlGen.Emit(OpCodes.Ldfld, serviceInstanceFieldBuilder);
                    for (int a = 0; a < parameters.Length; a++)
                    {
                        methodIlGen.Emit(OpCodes.Ldarg, a + 1);
                    }

                    methodIlGen.Emit(OpCodes.Callvirt, mi);
                    //methodIlGen.Emit(OpCodes.Box, mi.ReturnType);
                    #endregion this.service.Login("","");

                    methodIlGen.Emit(OpCodes.Ret);
                }

                #endregion Methods

                TypeInfo proxyTypeInfo = typeBuilder.CreateTypeInfo();

                proxyType = proxyTypeInfo.AsType();

                memoryCache.Set(name, proxyType, DateTimeOffset.Now.AddHours(2));
            }

            return proxyType;
        }

        public static Dictionary<string, ProxyMethod> CreateMethodInvokers(Type serviceType)
        {
            MethodInfo[] serviceMethods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Dictionary<string, ProxyMethod> proxyMethodsDict = new Dictionary<string, ProxyMethod>();

            ProxyMethod proxyMethod;

            foreach (MethodInfo method in serviceMethods)
            {
                proxyMethod = new ProxyMethod { Method = method, MethodInvokerDelegate = ILUtility.CreateMethodInvokerDelegate(method) };
                proxyMethodsDict.Add($"{method.Name}_{method.GetHashCode()}", proxyMethod);
            }

            return proxyMethodsDict;
        }
    }
}