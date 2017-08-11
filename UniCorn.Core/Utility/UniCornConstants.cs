using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace UniCorn.Core
{
    public static class UniCornConstants
    {
        public static readonly Type TypeType = typeof(Type);
        public static readonly Type NullableType = typeof(Nullable<>);
        public static readonly Type ObjectType = typeof(object);

        public static readonly Type StringType = typeof(string);
        public static readonly Type BoolType = typeof(bool);
        public static readonly Type DateTimeType = typeof(DateTime);
        public static readonly Type TimeSpanType = typeof(TimeSpan);
        public static readonly Type GuidType = typeof(Guid);
        public static readonly Type TypeCodeType = typeof(TypeCode);
        public static readonly Type UniExtensionsType = typeof(UniCornExtensions);
        public static readonly Type IEnumerableType = typeof(IEnumerable);
        public static readonly Type ExpandoDictType = typeof(IDictionary<string, object>);
        public static readonly Type ExpandoObjectType = typeof(ExpandoObject);
        public static readonly Type NameValueCollectionType = typeof(NameValueCollection);

        public static readonly Type CSharpBinderType = typeof(InvokeMemberBinder).GetTypeInfo().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
        public static readonly PropertyInfo CSharpBinderTypeArgumentsPropInfo = null;// CSharpBinderType.GetProperty("TypeArguments");

        public static readonly FieldInfo DateTimeMinValue = DateTimeType.GetField("MinValue");
        public static readonly FieldInfo TimeSpanMinValue = TimeSpanType.GetField("MinValue");

        public static readonly ConstructorInfo ObjectCi = ObjectType.GetConstructor(Type.EmptyTypes);
        public static readonly MethodInfo GetTypeFromHandleMi = TypeType.GetMethod("GetTypeFromHandle");

        public static readonly CultureInfo EnUs = new CultureInfo("en-US");

        public static readonly Type DataReaderType = typeof(DbDataReader);
        public static readonly MethodInfo DataReaderIsDbNullMethod = DataReaderType.GetMethod("IsDBNull", new Type[] { typeof(int) });
        public static readonly MethodInfo DataReaderGetValueMethod = DataReaderType.GetMethod("GetValue", new Type[] { typeof(int) });
        public static readonly MethodInfo DataReaderReadMethod = DataReaderType.GetMethod("Read", new Type[] { });

        public static readonly MethodInfo ExpandoObjectDictAddMethod = ExpandoDictType.GetMethod("Add", new Type[] { typeof(string), typeof(object) });


        public static readonly MethodInfo ConvertToBoolean = typeof(Convert).GetMethod("ToBoolean", new Type[] { typeof(object) });

        public static readonly TypeInfo LambdaExpressionTypeInfo = typeof(LambdaExpression).GetTypeInfo();

        public static readonly TypeInfo GenericDelegateTypeInfo = typeof(Func<object[], object>).GetTypeInfo();
    }
}