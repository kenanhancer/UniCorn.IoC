using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace UniCorn.Core
{
    public class ILUtility
    {
        public static Func<object[], object> CreateMethodInvokerDelegate(MethodInfo method)
        {
            List<Type> prmList = method.GetParameters().Select(f => f.ParameterType).ToList();
            if (!method.IsStatic)
                prmList.Insert(0, method.DeclaringType);

            DynamicMethod dynamicMethod = new DynamicMethod("DynamicMethod", typeof(object), new Type[] { typeof(object[]) }, method.DeclaringType.GetTypeInfo().Module, skipVisibility: true);

            ILGenerator methodIlGen = dynamicMethod.GetILGenerator();

            Type prmType;

            for (int a = 0; a < prmList.Count; a++)
            {
                methodIlGen.Emit(OpCodes.Ldarg_0);
                methodIlGen.Emit(OpCodes.Ldc_I4, a);
                methodIlGen.Emit(OpCodes.Ldelem_Ref);

                prmType = prmList[a];

                if (prmType.GetTypeInfo().IsClass)
                    methodIlGen.Emit(OpCodes.Castclass, prmType);
                else
                    methodIlGen.Emit(OpCodes.Unbox_Any, prmType);
            }

            if (method.IsStatic)
                methodIlGen.Emit(OpCodes.Call, method);
            else
                methodIlGen.Emit(OpCodes.Callvirt, method);

            if (method.ReturnType == typeof(void))
                methodIlGen.Emit(OpCodes.Ldnull);
            else
                methodIlGen.Emit(OpCodes.Box, method.ReturnType);

            methodIlGen.Emit(OpCodes.Ret);

            return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
        }

        public static Func<object[], object> CreateInstanceDelegate(Type type)
        {
            var dynamicMethod = new DynamicMethod(type.Name, typeof(object), new Type[] { typeof(object[]) }, typeof(UniCornExtensions).GetTypeInfo().Module, skipVisibility: true);

            ILGenerator methodIlGen = dynamicMethod.GetILGenerator();

            bool isConstructorEmit = EmitNewObjOpCode(methodIlGen, type);

            if (!isConstructorEmit)
            {
                throw new NotSupportedException(string.Format("There is no constructor for {0}.", type.Name));
            }

            methodIlGen.Emit(OpCodes.Ret);

            return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
        }

        private static bool EmitNewObjOpCode(ILGenerator methodIlGen, Type type)
        {
            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
            {
                constructor = type.GetConstructors().FirstOrDefault();

                if (constructor != null)
                {
                    ParameterInfo[] parameters = constructor.GetParameters();
                    ParameterInfo prm;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        prm = parameters[i];

                        if (prm.ParameterType == type)
                        {
                            methodIlGen.Emit(OpCodes.Ldnull);
                        }
                        else
                        {
                            methodIlGen.Emit(OpCodes.Ldarg_0);
                            methodIlGen.Emit(OpCodes.Ldc_I4, i);
                            methodIlGen.Emit(OpCodes.Ldelem_Ref);

                            if (prm.ParameterType.GetTypeInfo().IsClass)
                                methodIlGen.Emit(OpCodes.Castclass, prm.ParameterType);
                            else
                                methodIlGen.Emit(OpCodes.Unbox_Any, prm.ParameterType);
                        }
                    }
                }
            }

            if (constructor != null)
            {
                methodIlGen.Emit(OpCodes.Newobj, constructor);
                return true;
            }

            return false;
        }

        private static void EmitLoadConstantInt(ILGenerator methodIlGen, int i)
        {
            switch (i)
            {
                case 0:
                    methodIlGen.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    methodIlGen.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    methodIlGen.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    methodIlGen.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    methodIlGen.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    methodIlGen.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    methodIlGen.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    methodIlGen.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    methodIlGen.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    methodIlGen.Emit(OpCodes.Ldc_I4, i);
                    break;
            }
        }
    }
}