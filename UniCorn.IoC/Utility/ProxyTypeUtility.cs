using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UniCorn.BaseLibrary;
using UniCorn.Core;

namespace UniCorn.IoC
{
    public static class ProxyTypeUtility
    {
        static ConcurrentDictionary<Type, Type> concTypes = new ConcurrentDictionary<Type, Type>();

        public static Type CreateProxyType(Type typeToProxy)
        {
            //string name = $"{typeToProxy.FullName}_ProxyType";

            Type generatedProxyType = concTypes.GetOrAdd(typeToProxy, f =>
            {
                return GenerateProxyType(f);
            });

            return generatedProxyType;
        }

        private static Type GenerateProxyType(Type typeToProxy)
        {
            Type implementType = typeToProxy;
            
            MethodInfo[] serviceMethods = implementType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            string typeName = $"{implementType.Name}_Proxy";

            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = asmBuilder.DefineDynamicModule("DynamicModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            typeBuilder.AddInterfaceImplementation(IoCConstants.proxyInterfaceType);

            foreach (CustomAttributeData attr in implementType.CustomAttributes)
            {
                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(attr.Constructor,
                                               attr.ConstructorArguments.Select(f => f.Value).ToArray(),
                                               attr.NamedArguments.Select(f => f.TypedValue.ArgumentType.GetProperty(f.MemberName)).ToArray(),
                                               attr.NamedArguments.Select(f => f.TypedValue.Value).ToArray()));
            }

            if (implementType.IsInterface)
                typeBuilder.AddInterfaceImplementation(typeToProxy);
            else
                typeBuilder.SetParent(typeToProxy);

            //ConstructorBuilder firstConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { interceptorType });

            ConstructorBuilder secondConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { IoCConstants.interceptorType, implementType });

            ConstructorBuilder staticConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

            List<FieldBuilder> proxyMethodVariableFieldBuilderList = new List<FieldBuilder>();
            Label equityLabel;
            Label elseLabel;
            Label equityLabel2;
            Label elseLabel2;

            MethodInfo mi;

            #region Static Constructor

            ILGenerator staticConstructorIlGen = staticConstructorBuilder.GetILGenerator();

            staticConstructorIlGen.Emit(OpCodes.Ldtoken, implementType);
            staticConstructorIlGen.Emit(OpCodes.Call, BaseTypeConstants.GetTypeFromHandleMi);
            staticConstructorIlGen.Emit(OpCodes.Call, IoCConstants.getProxyMethodsMi);

            LocalBuilder proxyMethodsTypeLocalBuilder = staticConstructorIlGen.DeclareLocal(IoCConstants.proxyMethodsType);
            staticConstructorIlGen.Emit(OpCodes.Stloc, proxyMethodsTypeLocalBuilder);

            FieldBuilder proxyMethodVariableFieldBuilder;
            LocalBuilder objVariableLocalBuilder = staticConstructorIlGen.DeclareLocal(BaseTypeConstants.ObjectType);

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
            constructorSecondILGen.Emit(OpCodes.Call, BaseTypeConstants.ObjectCi);

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

                MethodAttributes attribute = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual;

                if (mi.IsVirtual && !mi.IsFinal)
                    attribute |= MethodAttributes.ReuseSlot;
                else
                    attribute |= MethodAttributes.NewSlot;

                methodBuilder = typeBuilder.DefineMethod(mi.Name, attribute, mi.ReturnType, parameters.Select(f => f.ParameterType).ToArray());

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
                            genericParameter.SetGenericParameterAttributes(item.GenericParameterAttributes);
                        }
                    }
                }

                methodIlGen = methodBuilder.GetILGenerator();

                #region if (this.interceptor != null)

                #region if (this.interceptor != null)

                methodIlGen.Emit(OpCodes.Ldarg_0);
                methodIlGen.Emit(OpCodes.Ldfld, interceptorFieldBuilder);
                methodIlGen.Emit(OpCodes.Ldnull);
                methodIlGen.Emit(OpCodes.Cgt_Un);
                equityLabel = methodIlGen.DefineLabel();
                methodIlGen.Emit(OpCodes.Brfalse, equityLabel);

                methodIlGen.Emit(OpCodes.Nop);

                #endregion if (this.interceptor != null)

                #region object[] parameters = new object[]{firstName, lastName};

                methodIlGen.Emit(OpCodes.Ldc_I4, parameters.Length);
                methodIlGen.Emit(OpCodes.Newarr, BaseTypeConstants.ObjectType);
                paramArrayLocalBuilder = methodIlGen.DeclareLocal(typeof(object[]));
                methodIlGen.Emit(OpCodes.Stloc, paramArrayLocalBuilder);

                for (int a = 0; a < parameters.Length; a++)
                {
                    methodIlGen.Emit(OpCodes.Ldloc, paramArrayLocalBuilder);
                    methodIlGen.Emit(OpCodes.Ldc_I4, a);
                    methodIlGen.Emit(OpCodes.Ldarg, a + 1);
                    methodIlGen.Emit(OpCodes.Stelem_Ref);
                }

                #endregion object[] parameters = new object[]{firstName, lastName};

                #region IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy.DbType_ProxyMethod);

                methodIlGen.Emit(OpCodes.Ldloc, paramArrayLocalBuilder);
                methodIlGen.Emit(OpCodes.Ldarg_0);
                methodIlGen.Emit(OpCodes.Ldfld, serviceInstanceFieldBuilder);
                methodIlGen.Emit(OpCodes.Ldsfld, proxyMethodVariableFieldBuilderList[i]);

                methodIlGen.Emit(OpCodes.Newobj, IoCConstants.standartInvocationCi);
                invocationLocalBuilder = methodIlGen.DeclareLocal(IoCConstants.invocationType);
                methodIlGen.Emit(OpCodes.Stloc, invocationLocalBuilder);

                #endregion IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy.DbType_ProxyMethod);

                #region this.interceptor.Intercept(ınvocation);

                methodIlGen.Emit(OpCodes.Ldarg_0);
                methodIlGen.Emit(OpCodes.Ldfld, interceptorFieldBuilder);
                methodIlGen.Emit(OpCodes.Ldloc, invocationLocalBuilder);
                methodIlGen.Emit(OpCodes.Callvirt, IoCConstants.interceptMi);

                #endregion this.interceptor.Intercept(ınvocation);

                #region return (this.service == null) ? DatabaseTypee.Oracle : ((DatabaseTypee)ınvocation.ReturnValue);

                if (mi.ReturnType != BaseTypeConstants.VoidType)
                {

                    #region if(invocation.ReturnValue!=null)

                    //methodIlGen.Emit(OpCodes.Ldarg_0);
                    methodIlGen.Emit(OpCodes.Ldloc, invocationLocalBuilder);
                    methodIlGen.Emit(OpCodes.Callvirt, IoCConstants.invocationTypeReturnValueMi);
                    methodIlGen.Emit(OpCodes.Ldnull);
                    methodIlGen.Emit(OpCodes.Cgt_Un);
                    equityLabel2 = methodIlGen.DefineLabel();
                    methodIlGen.Emit(OpCodes.Brfalse, equityLabel2);

                    methodIlGen.Emit(OpCodes.Nop);

                    methodIlGen.Emit(OpCodes.Ldloc, invocationLocalBuilder);
                    methodIlGen.Emit(OpCodes.Callvirt, IoCConstants.invocationTypeReturnValueMi);

                    if (mi.ReturnType.IsClass)
                        methodIlGen.Emit(OpCodes.Castclass, mi.ReturnType);
                    else
                        methodIlGen.Emit(OpCodes.Unbox_Any, mi.ReturnType);

                    methodIlGen.Emit(OpCodes.Nop);

                    elseLabel2 = methodIlGen.DefineLabel();

                    methodIlGen.Emit(OpCodes.Br, elseLabel2);

                    methodIlGen.MarkLabel(equityLabel2);

                    #endregion if(invocation.ReturnValue!=null)

                    #region else

                    methodIlGen.Emit(OpCodes.Nop);
                    if (!ReflectionUtility.EmitPrimitiveOpCode(methodIlGen, mi.ReturnType))
                    {
                        methodIlGen.Emit(OpCodes.Ldnull);
                        methodIlGen.Emit(OpCodes.Nop);
                    }

                    methodIlGen.MarkLabel(elseLabel2);

                    #endregion else

                }

                #endregion return (this.service == null) ? DatabaseTypee.Oracle : ((DatabaseTypee)ınvocation.ReturnValue);

                #region }

                methodIlGen.Emit(OpCodes.Ret);

                methodIlGen.Emit(OpCodes.Nop);

                methodIlGen.MarkLabel(equityLabel);

                #endregion }

                #endregion if (this.interceptor != null)

                #region return (this.service == null) ? null : this.service.GetTableColumns(text);

                #region if(this.service!=null)



                methodIlGen.Emit(OpCodes.Ldarg_0);
                methodIlGen.Emit(OpCodes.Ldfld, serviceInstanceFieldBuilder);
                methodIlGen.Emit(OpCodes.Ldnull);
                methodIlGen.Emit(OpCodes.Cgt_Un);
                equityLabel = methodIlGen.DefineLabel();
                methodIlGen.Emit(OpCodes.Brfalse, equityLabel);

                methodIlGen.Emit(OpCodes.Nop);

                methodIlGen.Emit(OpCodes.Ldarg_0);
                methodIlGen.Emit(OpCodes.Ldfld, serviceInstanceFieldBuilder);
                for (int a = 0; a < parameters.Length; a++)
                {
                    methodIlGen.Emit(OpCodes.Ldarg, a + 1);
                }

                methodIlGen.Emit(OpCodes.Callvirt, mi);

                methodIlGen.Emit(OpCodes.Nop);

                elseLabel = methodIlGen.DefineLabel();

                methodIlGen.Emit(OpCodes.Br, elseLabel);

                methodIlGen.MarkLabel(equityLabel);

                #endregion if(this.service!=null)

                #region else

                methodIlGen.Emit(OpCodes.Nop);
                if (mi.ReturnType != BaseTypeConstants.VoidType && !ReflectionUtility.EmitPrimitiveOpCode(methodIlGen, mi.ReturnType))
                {
                    methodIlGen.Emit(OpCodes.Ldnull);
                    methodIlGen.Emit(OpCodes.Nop);
                }

                methodIlGen.MarkLabel(elseLabel);

                #endregion else

                #endregion return (this.service == null) ? null : this.service.GetTableColumns(text);

                methodIlGen.Emit(OpCodes.Ret);
            }

            #endregion Methods

            TypeInfo proxyTypeInfo = typeBuilder.CreateTypeInfo();

            Type proxyType = proxyTypeInfo.AsType();

            return proxyType;
        }

        public static Dictionary<string, ProxyMethod> CreateMethodInvokers(Type serviceType)
        {
            MethodInfo[] serviceMethods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Dictionary<string, ProxyMethod> proxyMethodsDict = new Dictionary<string, ProxyMethod>();

            ProxyMethod proxyMethod;

            foreach (MethodInfo method in serviceMethods)
            {
                proxyMethod = new ProxyMethod { Method = method, MethodInvokerDelegate = ReflectionUtility.CreateMethodInvokerDelegate(method) };
                proxyMethodsDict.Add($"{method.Name}_{method.GetHashCode()}", proxyMethod);
            }

            return proxyMethodsDict;
        }

        public static TProxy CreateProxyInstance<TProxy>(TProxy target, IInterceptor interceptor)
        {
            Type interfaceType = typeof(TProxy);

            return (TProxy)CreateProxyInstance(interfaceType, target, interceptor);
        }

        public static object CreateProxyInstance(Type proxyType, object target, IInterceptor interceptor)
        {
            if (proxyType == null)
                throw new ArgumentNullException("proxyType");

            Type generatedProxyType = CreateProxyType(proxyType);

            Func<object[], object> instanceDelegate = ReflectionUtility.CreateInstanceDelegate(generatedProxyType);

            return instanceDelegate(new[] { interceptor, target });
        }
    }
}