using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace UniCorn.Core
{
    public class PropertyDetailedInfo
    {
        public PropertyInfo PropertyInfo { get; set; }
        public Action<object, object> PropertySetter { get; set; }
    }
    public static class UniCornExtensions
    {
        #region Field Members
        private static string[] noArray = { "0", "off", "no" };
        private static string[] yesArray = { "1", "on", "yes" };
        #endregion Field Members

        static IMemoryCache memoryCache;

        static UniCornExtensions()
        {
            memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        public static bool IsAnonymous(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type type = null;
            if (obj is Type)
                type = (Type)obj;
            else
                type = obj.GetType();
            return type.Namespace == null;
        }

        public static bool IsNullableType(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException("type");
            return (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == UniCornConstants.NullableType);
        }

        public static string GetValueIfNotNullorEmpty(this object obj)
        {
            string objValue = obj as string;
            if (!String.IsNullOrEmpty(objValue))
                return objValue;
            return null;
        }

        public static Dictionary<string, PropertyDetailedInfo> GetPropertyDict(this object obj, bool fieldNameLower = false)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type convertionType = obj as Type;
            if (convertionType == null)
                convertionType = obj.GetType();
            TypeInfo convertionTypeInfo = convertionType.GetTypeInfo();

            string convertionTypeName = convertionTypeInfo.IsAnonymous() ? ((dynamic)convertionType.TypeHandle).Value.ToString() : convertionType.Name;
            string cacheName = $"{convertionTypeName}_TypeProperties";
            Dictionary<string, PropertyDetailedInfo> propertyDict = memoryCache.Get(cacheName) as Dictionary<string, PropertyDetailedInfo>;
            if (propertyDict == null)
            {
                propertyDict = new Dictionary<string, PropertyDetailedInfo>();
                string propertyName = null;
                Action<object, object> propertySetter = null;
                foreach (PropertyInfo pi in convertionType.GetProperties())
                {
                    if (pi.GetIndexParameters().Length > 0) continue;
                    propertyName = pi.Name;

                    propertyName = fieldNameLower ? propertyName.ToLowerInvariant() : propertyName;

                    if (!convertionType.IsAnonymous())
                        propertySetter = TypeUtility.CreatePropertySetDelegate(pi);

                    propertyDict.Add(propertyName, new PropertyDetailedInfo { PropertyInfo = pi, PropertySetter = propertySetter });
                }
                memoryCache.Set(cacheName, propertyDict, DateTimeOffset.Now.AddHours(2));
            }
            return propertyDict;
        }

        public static dynamic ToExpando(this object obj, bool fieldNameLower = false)
        {
            if (obj == null)
                throw new ArgumentException("obj");
            Type objType = obj.GetType();
            TypeInfo objTypeInfo = objType.GetTypeInfo();
            TypeCode objTypeCode = Type.GetTypeCode(objType);
            if (objType == UniCornConstants.ExpandoObjectType || UniCornConstants.ExpandoDictType.IsAssignableFrom(objType) || (objTypeCode != TypeCode.Object && Enum.IsDefined(UniCornConstants.TypeCodeType, objTypeCode))) return obj;
            dynamic result = new ExpandoObject();
            var resultDict = result as IDictionary<string, object>;
            object value = null;
            string propName;

            if (obj is IDataReader)
            {
                IDataReader dr = obj as IDataReader;
                for (int x = 0; x < dr.FieldCount; x++)
                {
                    propName = fieldNameLower ? dr.GetName(x).ToLowerInvariant() : dr.GetName(x);
                    value = dr[x];
                    resultDict.Add(propName, DBNull.Value.Equals(value) ? null : value);
                }
            }
            else if (obj is IEnumerable)
            {
                IEnumerable objAsEnumerable = obj as IEnumerable;
                IEnumerator objEnumerator = objAsEnumerable.GetEnumerator();
                IDictionary objDict = obj as IDictionary;
                bool isObjDict = (objDict != null);
                int i = 0;
                TypeCode currentTypeCode;
                object currentItem = null;
                IDictionary<string, object> anonymousDict;
                while (objEnumerator.MoveNext())
                {
                    currentItem = objEnumerator.Current;
                    if (isObjDict)
                    {
                        var dictEntry = (KeyValuePair<string, object>)objEnumerator.Current;
                        value = dictEntry.Value;
                        resultDict.Add(dictEntry.Key, value);
                    }
                    else if (currentItem.IsAnonymous())
                    {
                        anonymousDict = currentItem.ToExpando() as IDictionary<string, object>;

                        foreach (var item in anonymousDict)
                        {
                            value = item.Value;
                            resultDict.Add(item.Key + i.ToString(), value);
                        }
                    }
                    else
                    {
                        currentTypeCode = Type.GetTypeCode(objEnumerator.Current.GetType());
                        if (currentTypeCode != TypeCode.Object && Enum.IsDefined(UniCornConstants.TypeCodeType, currentTypeCode))
                            resultDict.Add((i).ToString(), objEnumerator.Current);
                    }
                    i++;
                }
            }
            else
            {
                Dictionary<string, PropertyDetailedInfo> propertyDict = obj.GetPropertyDict(fieldNameLower);
                PropertyInfo pi;
                foreach (var item in propertyDict)
                {
                    pi = item.Value.PropertyInfo;
                    propName = item.Key;
                    value = pi.GetValue(obj, null);
                    resultDict.Add(propName, value);
                }
            }
            return result;
        }

        public static T To<T>(this object obj, T defaultValue = default(T), bool fieldNameLower = false) where T : new()
        {
            if (obj == null || obj == DBNull.Value)
                return defaultValue;
            if (obj is T)
                return (T)obj;
            string objString = obj.ToString();
            Type objType = obj.GetType();
            TypeCode objTypeCode = Type.GetTypeCode(objType);
            Type convertionType = typeof(T);
            TypeInfo convertionTypeInfo = convertionType.GetTypeInfo();
            if (convertionTypeInfo.IsNullableType())
                convertionType = convertionType.GetGenericArguments()[0];
            var objDict = obj as IDictionary<string, object>;
            if (convertionTypeInfo.IsEnum)
            {
                try
                {
                    return (T)Enum.Parse(convertionType, objString, true);
                }
                catch
                {
                }
            }
            else if (convertionType == UniCornConstants.GuidType)
            {
                Guid guid;
                if (Guid.TryParse(objString, out guid))
                    return (T)((object)guid);
            }
            else if (convertionType == UniCornConstants.TimeSpanType)
            {
                TimeSpan timespan;
                if (TimeSpan.TryParse(objString, out timespan))
                    return (T)((object)timespan);
            }
            else if (Type.GetTypeCode(convertionType) == TypeCode.Object)
            {
                if (objDict == null)
                    objDict = obj.ToExpando(fieldNameLower: fieldNameLower) as IDictionary<string, object>;
                if (objDict != null && convertionType == UniCornConstants.ExpandoObjectType)
                    return (T)objDict;

                //T retValue = NewInstance<T>.InstanceMethod();
                T retValue = new T();
                var propertyDict = convertionType.GetPropertyDict();
                PropertyInfo pi = null;
                string propName;
                Action<object, object> propertySetter;
                foreach (var item in propertyDict)
                {
                    pi = item.Value.PropertyInfo;
                    propName = item.Key;
                    propertySetter = item.Value.PropertySetter;
                    if (propertySetter != null)
                    {
                        object val;
                        if (objDict.TryGetValue(propName, out val) && val != null)
                            propertySetter(retValue, val);
                    }
                }
                obj = retValue;
                return (T)obj;
            }
            else if (objTypeCode != TypeCode.Object && Enum.IsDefined(UniCornConstants.TypeCodeType, objTypeCode))
            {
                try
                {
                    return (T)Convert.ChangeType(obj, convertionType);
                }
                catch
                {
                }
            }
            else if (convertionType == UniCornConstants.BoolType)
            {
                if (yesArray.Contains(objString))
                    return (T)((object)true);
                else if (noArray.Contains(objString))
                    return (T)((object)false);
            }
            //return (T)Convert.ChangeType(objString, convertionType);
            return defaultValue;
        }

        public static Func<DbDataReader, ExpandoObject> ToExpandoObjectMapMethod(this DbDataReader reader)
        {
            if (reader == null)
                return null;

            Type expandoObjectT = UniCornConstants.ExpandoObjectType;
            Type expandoObjectDictT = UniCornConstants.ExpandoDictType;
            Type readerT = UniCornConstants.DataReaderType;
            MethodInfo expandoObjectDictAddMi = UniCornConstants.ExpandoObjectDictAddMethod;

            DynamicMethod dm = new DynamicMethod("DataReaderToEntityMap", expandoObjectT, new Type[] { readerT });

            ILGenerator il = dm.GetILGenerator();

            il.DeclareLocal(expandoObjectT);
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(expandoObjectDictT);
            il.DeclareLocal(typeof(object));
            LocalBuilder isDbNull = il.DeclareLocal(typeof(bool));

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Newobj, expandoObjectT.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Stloc_2);
            //il.Emit(OpCodes.Ldloc_2);

            int ordinal = 0;
            string name;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                name = reader.GetName(i);
                ordinal = reader.GetOrdinal(name);
                Label equality = il.DefineLabel();

                il.Emit(OpCodes.Ldc_I4, ordinal);
                il.Emit(OpCodes.Stloc_1);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderIsDbNullMethod);
                il.Emit(OpCodes.Stloc, isDbNull);
                il.Emit(OpCodes.Ldloc, isDbNull);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, equality);

                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_1);

                il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderGetValueMethod);

                il.Emit(OpCodes.Stloc_3);
                il.Emit(OpCodes.Ldloc_2);

                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldloc_3);
                il.Emit(OpCodes.Callvirt, expandoObjectDictAddMi);

                il.Emit(OpCodes.Nop);
                il.MarkLabel(equality);

                Label elseLabel = il.DefineLabel();

                il.Emit(OpCodes.Ldloc, isDbNull);
                il.Emit(OpCodes.Brfalse, elseLabel);

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldloc_2);
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, expandoObjectDictAddMi);
                il.Emit(OpCodes.Nop);

                il.MarkLabel(elseLabel);
            }

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            Func<DbDataReader, ExpandoObject> dynamicMethodDelegate = (Func<DbDataReader, ExpandoObject>)dm.CreateDelegate(typeof(Func<DbDataReader, ExpandoObject>));

            return dynamicMethodDelegate;
        }

        public static Func<DbDataReader, T> ToEntityMapMethod<T>(this DbDataReader reader) where T : new()
        {
            if (reader == null)
                return null;

            Type typeT = typeof(T);
            TypeCode typeTCode = Type.GetTypeCode(typeT);
            Type readerT = UniCornConstants.DataReaderType;
            PropertyInfo[] properties = typeT.GetProperties();

            DynamicMethod dm = new DynamicMethod("DataReaderToEntityMap", typeT, new Type[] { readerT }, typeT);

            ILGenerator il = dm.GetILGenerator();

            il.DeclareLocal(typeT);
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(typeof(bool));

            il.Emit(OpCodes.Nop);

            Type propertyUnderlyingType;
            string propertyName;

            if (typeTCode != TypeCode.Object && Enum.IsDefined(UniCornConstants.TypeCodeType, typeTCode))
            {
                Label equality = il.DefineLabel();

                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_1);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderIsDbNullMethod);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, equality);

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_1);

                if (typeT == typeof(bool))
                {
                    il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderGetValueMethod);
                    il.Emit(OpCodes.Call, UniCornConstants.ConvertToBoolean);
                }
                else
                {
                    propertyUnderlyingType = Nullable.GetUnderlyingType(typeT);
                    propertyName = propertyUnderlyingType != null ? propertyUnderlyingType.Name : typeT.Name;

                    il.Emit(OpCodes.Callvirt, readerT.GetMethod($"Get{propertyName}", new Type[] { typeof(int) }));

                    if (propertyUnderlyingType != null)
                    {
                        il.Emit(OpCodes.Newobj, typeT.GetConstructor(new Type[] { propertyUnderlyingType }));
                    }
                }

                il.Emit(OpCodes.Stloc_0);

                il.Emit(OpCodes.Nop);
                il.MarkLabel(equality);
            }
            else
            {
                il.Emit(OpCodes.Newobj, typeT.GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc_0);

                foreach (var pi in properties)
                {
                    Label equality = il.DefineLabel();

                    int ordinal;

                    try
                    {
                        ordinal = reader.GetOrdinal(pi.Name);
                    }
                    catch
                    {
                        continue;
                    }

                    il.Emit(OpCodes.Ldc_I4, ordinal);
                    il.Emit(OpCodes.Stloc_1);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderIsDbNullMethod);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brfalse, equality);

                    il.Emit(OpCodes.Nop);

                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_1);

                    if (pi.PropertyType.Name == "Object")
                        il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderGetValueMethod);
                    else
                    {
                        propertyUnderlyingType = Nullable.GetUnderlyingType(pi.PropertyType);
                        propertyName = propertyUnderlyingType != null ? propertyUnderlyingType.Name : pi.PropertyType.Name;

                        il.Emit(OpCodes.Callvirt, readerT.GetMethod($"Get{propertyName}", new Type[] { typeof(int) }));

                        if (propertyUnderlyingType != null)
                        {
                            il.Emit(OpCodes.Newobj, pi.PropertyType.GetConstructor(new Type[] { propertyUnderlyingType }));
                        }
                    }


                    il.Emit(OpCodes.Callvirt, pi.GetSetMethod());
                    il.Emit(OpCodes.Nop);
                    il.MarkLabel(equality);
                }
            }

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            Func<DbDataReader, T> dynamicMethodDelegate = (Func<DbDataReader, T>)dm.CreateDelegate(typeof(Func<DbDataReader, T>));


            return dynamicMethodDelegate;
        }

        public static Func<DbDataReader, List<T>> ToEntityListMapMethod<T>(this DbDataReader reader)
        {
            if (reader == null)
                return null;

            Type typeT = typeof(T);
            Type typeListT = typeof(List<T>);
            Type readerT = UniCornConstants.DataReaderType;
            Type[] methodArgs = { readerT };
            PropertyInfo[] properties = typeT.GetProperties();
            MethodInfo listAddMi = typeListT.GetMethod("Add", new Type[] { typeT });

            DynamicMethod dm = new DynamicMethod("DataReaderToEntityMap", typeListT, methodArgs);

            ILGenerator il = dm.GetILGenerator();

            Label startWhile = il.DefineLabel();
            Label checkWhile = il.DefineLabel();

            il.DeclareLocal(typeT);
            il.DeclareLocal(typeof(int));
            il.DeclareLocal(typeListT);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Newobj, typeListT.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_2);

            #region WhileBody
            il.Emit(OpCodes.Br, checkWhile);
            il.MarkLabel(startWhile);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Newobj, typeT.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0);

            int counter = 0;
            Label[] dbNullCheckLabels = new Label[properties.Length];
            Type propertyUnderlyingType;
            string propertyName;
            int ordinal = 0;

            foreach (var pi in properties)
            {
                try
                {
                    ordinal = reader.GetOrdinal(pi.Name);
                }
                catch
                {
                    continue;
                }

                dbNullCheckLabels[counter] = il.DefineLabel();


                il.Emit(OpCodes.Ldc_I4, ordinal);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderIsDbNullMethod);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, dbNullCheckLabels[counter]);

                //il.EmitWriteLine(pi.Name);

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_1);

                if (pi.PropertyType.Name == "Object")
                {
                    il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderGetValueMethod);
                }
                else
                {
                    propertyUnderlyingType = Nullable.GetUnderlyingType(pi.PropertyType);
                    propertyName = propertyUnderlyingType != null ? propertyUnderlyingType.Name : pi.PropertyType.Name;
                    il.Emit(OpCodes.Callvirt, readerT.GetMethod($"Get{propertyName}", new Type[] { typeof(int) }));
                    if (propertyUnderlyingType != null)
                    {
                        il.Emit(OpCodes.Newobj, pi.PropertyType.GetConstructor(new Type[] { propertyUnderlyingType }));
                    }
                }

                il.Emit(OpCodes.Callvirt, pi.GetSetMethod());

                il.MarkLabel(dbNullCheckLabels[counter]);

                il.Emit(OpCodes.Nop);

                counter++;
            }

            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Callvirt, listAddMi);

            il.Emit(OpCodes.Nop);

            il.MarkLabel(checkWhile);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, UniCornConstants.DataReaderReadMethod);
            il.Emit(OpCodes.Brtrue, startWhile);
            #endregion WhileBody

            il.Emit(OpCodes.Ldloc_2);

            il.Emit(OpCodes.Ret);

            var dynamicMethodDelegate = (Func<DbDataReader, List<T>>)dm.CreateDelegate(typeof(Func<DbDataReader, List<T>>));

            return dynamicMethodDelegate;
        }

        public static Delegate CreateDelegate(this MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (!method.IsStatic)
                throw new ArgumentException("The provided method must be static.", "method");

            if (method.IsGenericMethod)
                throw new ArgumentException("The provided method must not be generic.", "method");


            Type[] typeArgs = method.GetParameters().Select(f => f.ParameterType).Concat(new[] { method.ReturnType }).ToArray();

            Delegate result = method.CreateDelegate(Expression.GetDelegateType(typeArgs));

            return result;
        }

        public static Delegate CreateDelegateV2(this MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (!method.IsStatic)
                throw new ArgumentException("The provided method must be static.", "method");

            if (method.IsGenericMethod)
                throw new ArgumentException("The provided method must not be generic.", "method");

            var parameters = method.GetParameters()
                                   .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                                   .ToArray();

            MethodCallExpression methodCall = Expression.Call(null, method, parameters);

            Delegate result = Expression.Lambda(methodCall, parameters).Compile();

            return result;
        }

        public static T CreateDelegateV3<T>(this MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (!method.IsStatic)
                throw new ArgumentException("The provided method must be static.", "method");

            if (method.IsGenericMethod)
                throw new ArgumentException("The provided method must not be generic.", "method");

            var parameters = method.GetParameters()
                                   .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                                   .ToArray();

            MethodCallExpression methodCall = Expression.Call(null, method, parameters);

            T result = Expression.Lambda<T>(methodCall, parameters).Compile();

            return result;
        }
    }
}