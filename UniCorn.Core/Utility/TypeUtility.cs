using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace UniCorn.Core
{
    public abstract class EntityBase : INotifyPropertyChanged
    {
        private string key;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public string __Key { get { return key; } }

        protected void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public EntityBase()
        {
            key = Guid.NewGuid().ToString("N");
        }
    }

    public class TypeEntity
    {
        public string Name { get; set; }
        public Type DataType { get; set; }
        public bool AutoInitialize { get; set; }
        public List<TypeEntity> Fields { get; set; } = new List<TypeEntity>();
    }

    public static class InstanceFactory<T> where T : class, new()
    {
        public static readonly Func<object[], T> InstanceMethod;

        static InstanceFactory()
        {
            InstanceMethod = TypeUtility.CreateInstanceDelegate<T>();
        }
    }

    public static class TypeUtility
    {
        static ConcurrentDictionary<string, Delegate> memoryCache;

        static TypeUtility()
        {
            memoryCache = new ConcurrentDictionary<string, Delegate>();
        }

        public static Type CreatePocoType(TypeEntity typeEntity)
        {
            string name = typeEntity.Name;

            Type baseType = typeof(EntityBase);
            string assemblyName = "DynamicAssembly";
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = asmBuilder.DefineDynamicModule(assemblyName);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(name, TypeAttributes.Public, baseType);

            #region Serializable Attribute

            //var serializableCi = typeof(SerializableAttribute).GetConstructor(new Type[] { });
            //var serializableAttributeBuilder = new CustomAttributeBuilder(serializableCi, new object[] { });

            //typeBuilder.SetCustomAttribute(serializableAttributeBuilder);

            #endregion Serializable Attribute

            Type interfacePocoType = CreatePocoInterfaceType(typeEntity, moduleBuilder, typeBuilder);

            if (interfacePocoType != null)
                typeBuilder.AddInterfaceImplementation(interfacePocoType);

            Type objectType = typeof(object);
            ConstructorBuilder constructorBuilder = null;
            ILGenerator constructorILGen = null;
            FieldBuilder privateFieldBuilder;
            PropertyBuilder propertyBuilder;
            MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual;
            MethodBuilder propertyGetMethodBuilder;
            ILGenerator propertyGetMethodIL;

            MethodBuilder propertySetMethodBuilder;
            ILGenerator propertySetMethodIL;
            Type propertyType;
            System.Reflection.TypeInfo propertyTypeInfo;
            MethodInfo stringCompareMi = typeof(string).GetMethod("Compare", new Type[] { typeof(string), typeof(string) });

            PropertyBuilder indexerPropertyBuilder = typeBuilder.DefineProperty("Item", PropertyAttributes.None, CallingConventions.HasThis, objectType, new Type[] { typeof(string) });

            MethodBuilder indexerGetterMethodBuilder = typeBuilder.DefineMethod("get_Item", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, objectType, new Type[] { typeof(string) });

            MethodBuilder indexerSetterMethodBuilder = typeBuilder.DefineMethod("set_Item", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, null, new Type[] { typeof(string), objectType });

            var custNameGetIL = indexerGetterMethodBuilder.GetILGenerator();
            LocalBuilder retValLocalBuilder = custNameGetIL.DeclareLocal(objectType);
            var getterEnd = custNameGetIL.DefineLabel();

            var custNameSetIL = indexerSetterMethodBuilder.GetILGenerator();
            var setterEnd = custNameSetIL.DefineLabel();

            foreach (TypeEntity field in typeEntity.Fields)
            {
                propertyType = field.DataType;

                propertyTypeInfo = propertyType.GetTypeInfo();

                privateFieldBuilder = typeBuilder.DefineField("_" + field.Name, propertyType, FieldAttributes.Private);

                propertyBuilder = typeBuilder.DefineProperty(field.Name, PropertyAttributes.None, propertyType, null);

                #region PropertyGetMethod

                propertyGetMethodBuilder = typeBuilder.DefineMethod("get_" + field.Name, getSetAttr, propertyType, Type.EmptyTypes);

                propertyGetMethodIL = propertyGetMethodBuilder.GetILGenerator();

                propertyGetMethodIL.Emit(OpCodes.Ldarg_0);
                propertyGetMethodIL.Emit(OpCodes.Ldfld, privateFieldBuilder);
                propertyGetMethodIL.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(propertyGetMethodBuilder);

                #endregion PropertyGetMethod

                #region PropertySetMethod

                propertySetMethodBuilder = typeBuilder.DefineMethod("set_" + field.Name, getSetAttr, null, new Type[] { propertyType });

                propertySetMethodIL = propertySetMethodBuilder.GetILGenerator();

                propertySetMethodIL.Emit(OpCodes.Ldarg_0);
                propertySetMethodIL.Emit(OpCodes.Ldarg_1);
                propertySetMethodIL.Emit(OpCodes.Stfld, privateFieldBuilder);
                //propertySetMethodIL.Emit(OpCodes.Call, privateFieldBuilder);
                propertySetMethodIL.Emit(OpCodes.Ret);

                propertyBuilder.SetSetMethod(propertySetMethodBuilder);

                #endregion PropertySetMethod

                #region Constructor

                if (field.AutoInitialize)
                {
                    if (constructorBuilder == null)
                    {
                        constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                        constructorILGen = constructorBuilder.GetILGenerator();

                        constructorILGen.Emit(OpCodes.Ldarg_0);
                        constructorILGen.Emit(OpCodes.Call, UniCornConstants.ObjectCi);

                        constructorILGen.Emit(OpCodes.Ldarg_0);
                        constructorILGen.Emit(OpCodes.Call, baseType.GetConstructor(Type.EmptyTypes));
                    }

                    constructorILGen.Emit(OpCodes.Ldarg_0);

                    if (field.DataType.ToString() == name)
                        constructorILGen.Emit(OpCodes.Newobj, constructorBuilder);
                    else
                        constructorILGen.Emit(OpCodes.Newobj, propertyType.GetConstructor(Type.EmptyTypes));

                    constructorILGen.Emit(OpCodes.Call, propertySetMethodBuilder);

                    constructorILGen.Emit(OpCodes.Nop);
                }

                #endregion Constructor

                #region Indexer

                #region Indexer Getter Method

                Label equality = custNameGetIL.DefineLabel();

                custNameGetIL.Emit(OpCodes.Ldarg_1);
                custNameGetIL.Emit(OpCodes.Ldstr, field.Name);
                custNameGetIL.Emit(OpCodes.Call, stringCompareMi);
                custNameGetIL.Emit(OpCodes.Ldc_I4_0);
                custNameGetIL.Emit(OpCodes.Ceq);
                custNameGetIL.Emit(OpCodes.Brfalse, equality);

                custNameGetIL.Emit(OpCodes.Ldarg_0);
                custNameGetIL.Emit(OpCodes.Ldfld, privateFieldBuilder);
                if (propertyTypeInfo.IsValueType)
                    custNameGetIL.Emit(OpCodes.Box, propertyType);

                custNameGetIL.Emit(OpCodes.Stloc, retValLocalBuilder);

                custNameGetIL.Emit(OpCodes.Br, getterEnd);

                custNameGetIL.MarkLabel(equality);

                #endregion

                #region Indexer Setter Method

                Label setterEquality = custNameSetIL.DefineLabel();

                custNameSetIL.Emit(OpCodes.Ldarg_1);
                custNameSetIL.Emit(OpCodes.Ldstr, field.Name);
                custNameSetIL.Emit(OpCodes.Call, stringCompareMi);
                custNameSetIL.Emit(OpCodes.Ldc_I4_0);
                custNameSetIL.Emit(OpCodes.Ceq);
                custNameSetIL.Emit(OpCodes.Brfalse, setterEquality);

                custNameSetIL.Emit(OpCodes.Ldarg_0);
                custNameSetIL.Emit(OpCodes.Ldarg_2);
                custNameSetIL.Emit(OpCodes.Unbox_Any, propertyType);
                custNameSetIL.Emit(OpCodes.Stfld, privateFieldBuilder);

                custNameSetIL.Emit(OpCodes.Br, setterEnd);

                custNameSetIL.MarkLabel(setterEquality);

                #endregion Indexer Setter Method

                #endregion Indexer
            }

            #region Indexer

            #region Indexer Getter Method

            custNameGetIL.Emit(OpCodes.Ldnull);
            custNameGetIL.Emit(OpCodes.Stloc, retValLocalBuilder);
            custNameGetIL.Emit(OpCodes.Br_S, getterEnd);

            custNameGetIL.MarkLabel(getterEnd);
            custNameGetIL.Emit(OpCodes.Ldloc, retValLocalBuilder);
            custNameGetIL.Emit(OpCodes.Ret);
            indexerPropertyBuilder.SetGetMethod(indexerGetterMethodBuilder);

            #endregion Indexer Getter Method

            #region Indexer Setter Method

            custNameSetIL.MarkLabel(setterEnd);
            custNameSetIL.Emit(OpCodes.Ret);
            indexerPropertyBuilder.SetSetMethod(indexerSetterMethodBuilder);

            #endregion Indexer Setter Method

            #endregion Indexer

            #region Constructor

            if (constructorBuilder != null)
            {
                constructorILGen.Emit(OpCodes.Ret);
            }

            #endregion Constructor

            System.Reflection.TypeInfo pocoTypeInfo = typeBuilder.CreateTypeInfo();

            return pocoTypeInfo.AsType();
        }

        public static Type CreatePocoInterfaceType(TypeEntity typeEntity, ModuleBuilder moduleBuilder, TypeBuilder typeBuilder)
        {
            string name = "I" + typeEntity.Name;

            TypeBuilder interfaceBuilder = moduleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            PropertyBuilder interfacePropertyBuilder;
            Type propertyType;

            foreach (TypeEntity field in typeEntity.Fields)
            {
                propertyType = field.DataType;

                interfacePropertyBuilder = interfaceBuilder.DefineProperty(field.Name, PropertyAttributes.HasDefault, propertyType, null);

                interfacePropertyBuilder.SetGetMethod(interfaceBuilder.DefineMethod("get_" + field.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Virtual, propertyType, Type.EmptyTypes));

                interfacePropertyBuilder.SetSetMethod(interfaceBuilder.DefineMethod("set_" + field.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Virtual, null, new Type[] { propertyType }));
            }

            System.Reflection.TypeInfo interfacePocoTypeInfo = interfaceBuilder.CreateTypeInfo();

            return interfacePocoTypeInfo.AsType();
        }

        public static string GeneratePocoCode(TypeEntity typeEntity, bool includeIndexer = true, bool includeNotifyPropertyChanged = false)
        {
            StringBuilder sbType = new StringBuilder();
            StringBuilder sbIndexer = new StringBuilder();
            StringBuilder sbIndexGetter = new StringBuilder();
            StringBuilder sbIndexSetter = new StringBuilder();

            string baseType = includeNotifyPropertyChanged ? ": UniCorn.Core.EntityBase" : "";
            sbType.AppendLine($"public class {typeEntity.Name} {baseType}\n{{");

            sbIndexer.AppendLine("\tpublic object this[string propertyName]\n\t{");

            sbIndexGetter.AppendLine("\t\tget\n\t\t{");

            sbIndexSetter.AppendLine("\t\tset\n\t\t{");

            string fieldName;
            Type fieldType;
            string ifStatement = "if";
            for (int i = 0; i < typeEntity.Fields.Count; i++)
            {
                TypeEntity field = typeEntity.Fields[i];
                fieldName = field.Name;
                fieldType = field.DataType;

                if (includeNotifyPropertyChanged)
                {
                    sbType.AppendLine($"\tprivate {fieldType} _{fieldName};");
                    sbType.Append($"\tpublic {fieldType} {fieldName}{{");
                    sbType.Append($"get{{return _{fieldName};}}");
                    sbType.Append($"set{{base.SetField(ref _{fieldName}, value);}}");
                    sbType.Append("}\n");
                }
                else
                    sbType.AppendLine($"\tpublic {fieldType} {fieldName} {{ get; set; }}");


                if (i > 0)
                    ifStatement = "else if";

                sbIndexGetter.AppendLine($"\t\t\t{ifStatement} (propertyName == \"{fieldName}\")");
                sbIndexGetter.AppendLine($"\t\t\t\treturn \"{fieldName}\";");

                sbIndexSetter.AppendLine($"\t\t\t{ifStatement} (propertyName == \"{fieldName}\")");
                sbIndexSetter.AppendLine($"\t\t\t\t{fieldName} = ({fieldType})value;");
            }

            sbIndexGetter.AppendLine("\t\t\treturn null;\n\t\t}");
            sbIndexSetter.AppendLine("\t\t}");


            sbIndexer.Append(sbIndexGetter.ToString());
            sbIndexer.Append(sbIndexSetter.ToString());
            sbIndexer.Append("\t}");

            if (includeIndexer)
                sbType.Append(sbIndexer.ToString());

            sbType.Append("\n}");

            return sbType.ToString();
        }

        public static string GeneratePocoCode(DbDataReader reader, string typeName, bool includeIndexer = true, bool includeNotifyPropertyChanged = false)
        {
            TypeEntity typeEntity = new TypeEntity { Name = typeName };

            for (int i = 0; i < reader.FieldCount; i++)
                typeEntity.Fields.Add(new TypeEntity { Name = reader.GetName(i), DataType = reader.GetFieldType(i) });

            return GeneratePocoCode(typeEntity, includeIndexer, includeNotifyPropertyChanged);
        }

        public static Assembly CompileCode(string assemblyName, string csharpCode, out string errorMessage)
        {
            errorMessage = String.Empty;

            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName)
                                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                            .AddReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                                            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(csharpCode));


            StringBuilder message = new StringBuilder();

            using (var ms = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(ms);
                
                if (!emitResult.Success)
                {
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    errorMessage = message.ToString();
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

                    return assembly;
                }
            }

            return null;
        }

        public static Delegate CreateInstanceDelegate(Type type, Type delegateType)
        {
            string typeName = type.Name;
            string typeCacheName = $"{typeName}_CreateInstanceDynamicMethod";

            var dynamicMethod = new DynamicMethod(type.Name, typeof(object), new Type[] { typeof(object[]) }, typeof(UniCornExtensions).GetTypeInfo().Module, skipVisibility: true);

            ILGenerator il = dynamicMethod.GetILGenerator();

            bool isConstructorEmit = EmitNewObjOpCode(il, type);

            if (!isConstructorEmit)
                throw new NotSupportedException($"There is no constructor for {typeName}, this is not supported");

            il.Emit(OpCodes.Ret);

            Delegate dynamicMethodDelegate = dynamicMethod.CreateDelegate(delegateType);

            return dynamicMethodDelegate;
        }

        public static Func<object[], object> CreateInstanceDelegate(Type type)
        {
            return (Func<object[], object>)CreateInstanceDelegate(type, typeof(Func<object[], object>));
        }

        public static Func<object[], T> CreateInstanceDelegate<T>()
        {
            return (Func<object[], T>)CreateInstanceDelegate(typeof(T), typeof(Func<object[], T>));
        }

        public static Func<object[], object> GetAnonymousInstantiator(Type type)
        {
            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor == null) return null;

            var paramExpr = Expression.Parameter(typeof(object[]));

            return Expression.Lambda<Func<object[], object>>
            (
                Expression.New
                (
                    ctor,
                    ctor.GetParameters().Select
                    (
                        (x, i) => Expression.Convert
                        (
                            Expression.ArrayIndex(paramExpr, Expression.Constant(i)),
                            x.ParameterType
                        )
                    )
                ), paramExpr).Compile();
        }

        public static LambdaExpression GetAnonymousInstantiatorLambda(Type type)
        {
            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor == null) return null;

            ParameterInfo[] parameterList = ctor.GetParameters();

            List<ParameterExpression> parameterExpList = new List<ParameterExpression>();

            foreach (ParameterInfo prmInfo in parameterList)
            {
                parameterExpList.Add(Expression.Parameter(prmInfo.ParameterType));
            }

            return Expression.Lambda
            (
                Expression.New
                (
                    ctor, parameterExpList
                ),
                parameterExpList
            );
        }

        private static bool EmitNewObjOpCode(ILGenerator il, Type type)
        {
            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
            {
                constructor = type.GetConstructors().FirstOrDefault();

                if (constructor != null)
                {
                    var parameters = constructor.GetParameters();
                    bool isPrimitiveTypeEmit;
                    ParameterInfo prm;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        prm = parameters[i];

                        isPrimitiveTypeEmit = EmitPrimitiveOpCode(il, prm.ParameterType);
                        if (!isPrimitiveTypeEmit)
                        {
                            if (prm.ParameterType == type)
                            {
                                il.Emit(OpCodes.Ldnull);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldc_I4, i);
                                il.Emit(OpCodes.Ldelem_Ref);

                                il.Emit(OpCodes.Unbox_Any, prm.ParameterType);
                            }
                        }
                    }
                }
                else { }
            }

            if (constructor != null)
            {
                il.Emit(OpCodes.Newobj, constructor);
                return true;
            }

            return false;
        }

        public static bool EmitPrimitiveOpCode(ILGenerator il, Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            if (typeCode == TypeCode.Int32)
            {
                il.Emit(OpCodes.Ldc_I4_0);
                return true;
            }
            else if (typeCode == TypeCode.String)
            {
                il.Emit(OpCodes.Ldnull);
                return true;
            }
            else if (typeCode == TypeCode.DateTime)
            {
                il.Emit(OpCodes.Ldsfld, UniCornConstants.DateTimeMinValue);
                return true;
            }
            else if (type == UniCornConstants.TimeSpanType)
            {
                il.Emit(OpCodes.Ldsfld, UniCornConstants.TimeSpanMinValue);
                return true;
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                il.Emit(OpCodes.Ldc_I4_0);
                if (type.GetTypeInfo().IsClass)
                    il.Emit(OpCodes.Castclass, type);
                else
                    il.Emit(OpCodes.Unbox_Any, type);
                return true;
            }


            return false;
        }

        /// <summary>
        /// Person person = new Person();
        /// PropertyInfo firstName_pi = typeof(Person).GetProperty("FirstName");
        /// var propertySetter = UniCornTypeFactory.CreatePropertySetDelegate(firstName_pi);
        /// propertySetter(person, "Kenan");
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Action<object, object> CreatePropertySetDelegate(PropertyInfo propertyInfo)
        {
            MethodInfo setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                return null;
            Type[] arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);
            DynamicMethod setter = new DynamicMethod($"{propertyInfo.Name}_Set_", typeof(void), arguments, propertyInfo.DeclaringType);
            ILGenerator generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.Emit(OpCodes.Ldarg_1);
            if (propertyInfo.PropertyType.GetTypeInfo().IsClass)
                generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
            else
                generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
            generator.EmitCall(OpCodes.Callvirt, setMethod, null);
            generator.Emit(OpCodes.Ret);
            return (Action<object, object>)setter.CreateDelegate(typeof(Action<object, object>));
        }
    }
}