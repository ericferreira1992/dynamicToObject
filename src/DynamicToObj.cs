using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;

/*
Created by Eric Andrade Ferreira
08-10-2017
*/

namespace Solution.YourProject.Core
{
    public class DynamicToObj
    {
        public static T Mapper<T>(dynamic objIn)
        {
            var objOut = ProcessMapper(objIn, typeof(T));
            return objOut;
        }

        public static List<T> MapperList<T>(dynamic objListIn)
        {
            var tOut = typeof(T);
            var listOut = CreateInstanceList(tOut);
            foreach (var objIn in objListIn)
            {
                if (objIn != null)
                {
                    if (IsNativeType(tOut) || (tOut != null && tOut.BaseType == typeof(Enum)))
                        listOut.Add(objIn);
                    else
                        listOut.Add(ProcessMapper(objIn, tOut));
                }
            }
            return listOut as List<T>;
        }

        private static object Mapper(dynamic objIn, Type tOut)
        {
            var objOut = ProcessMapper(objIn, tOut);
            return objOut;
        }

        private static object MapperList(dynamic objListIn, Type tOut)
        {
            var listOut = CreateInstanceList(tOut);
            foreach (var objIn in objListIn)
            {
                if (objIn != null)
                {
                    if (IsNativeType(tOut) || (tOut != null && tOut.BaseType == typeof(Enum)))
                    {
                        try { listOut.Add(objIn); }
                        catch (Exception ex)
                        {
                            var typeStr = tOut != null ? ("(Type: " + tOut.Name + ")") : "";
                            throw new Exception(ex.Message + "\r\nProbable error: Object type does not match the other. " + typeStr);
                        }
                    }
                    else
                        listOut.Add(ProcessMapper(objIn, tOut));
                }
            }
            return listOut;
        }

        private static object MapperDictionary(dynamic objDictIn, Type tOut)
        {
            if (objDictIn != null && tOut != null)
            {
                var dictOut = CreateInstanceDictionary(tOut);
                if ((objDictIn as IDictionary).Count > 0)
                {
                    foreach (var dictIn in objDictIn)
                    {
                        //Key
                        var keyType = GetTypeKeyDictionary(dictIn);
                        object key = null;
                        if (IsNativeType(keyType) || (keyType != null && keyType.BaseType == typeof(Enum)))
                        {
                            try { key = dictIn.Key; }
                            catch (Exception ex)
                            {
                                var typeStr = keyType != null ? ("(Type: " + keyType.Name + ")") : "";
                                throw new Exception(ex.Message + "\r\nProbable error: Object type does not match the other. " + typeStr);
                            }
                        }
                        else
                            key = ProcessMapper(dictIn.Key, keyType);

                        //Value
                        var valueType = GetTypeKeyDictionary(dictIn);
                        object value = null;
                        if (IsNativeType(valueType) || (valueType != null && valueType.BaseType == typeof(Enum)))
                        {
                            try { value = dictIn.Value; }
                            catch (Exception ex)
                            {
                                var typeStr = valueType != null ? ("(Type: " + valueType.Name + ")") : "";
                                throw new Exception(ex.Message + "\r\nProbable error: Object type does not match the other. " + typeStr);
                            }
                        }
                        else
                            value = ProcessMapper(dictIn.Value, valueType);

                        dictOut.Add(key, value);
                    }
                }
                return dictOut;
            }
            else return null;
        }

        private static object ProcessMapper(dynamic objIn, Type tOut)
        {
            var objOut = Activator.CreateInstance(tOut);

            //Dynamic
            if (!(objIn is IDynamicMetaObjectProvider))
            {
                foreach (PropertyInfo propInfoIn in objIn.GetType().GetProperties())
                {
                    try
                    {
                        if (PropExists(objOut, propInfoIn.Name))
                        {
                            var valueObjIn = propInfoIn.GetValue(objIn, null);
                            var type = GetPropertyType(objOut, propInfoIn.Name);

                            //Dictionary
                            if(propInfoIn.PropertyType.Name.Contains("Dictionary"))
                            {
                                type = GetPropertyTypeDictionary(objOut, propInfoIn.Name);
                                var value = propInfoIn.GetValue(objIn, null);

                                if (value != null && (value as IDictionary).Count > 0)
                                {
                                    var valueOut = MapperDictionary(value, type);
                                    try { SetValue(objOut, propInfoIn.Name, valueOut); }
                                    catch { }
                                }
                                else
                                    try { SetValue(objOut, propInfoIn.Name, CreateInstanceDictionary(type)); }
                                    catch { }
                            }
                            //List
                            else if (propInfoIn.PropertyType.Namespace == "System.Collections.Generic")
                            {
                                type = GetPropertyTypeList(objOut, propInfoIn.Name);
                                var value = propInfoIn.GetValue(objIn, null);

                                if (value != null && (value as IEnumerable<object>).Count() > 0)
                                {
                                    var valueOut = MapperList(value, type);
                                    try { SetValue(objOut, propInfoIn.Name, valueOut); }
                                    catch { }
                                }
                                else
                                    try { SetValue(objOut, propInfoIn.Name, CreateInstanceList(type)); }
                                    catch { }
                            }
                            //Object
                            else
                            {
                                if (IsNativeType(type) || (type != null && type.BaseType == typeof(Enum)))
                                {
                                    try { SetValue(objOut, propInfoIn.Name, propInfoIn.GetValue(objIn, null)); }
                                    catch { }
                                }
                                else
                                {
                                    var valueOut = (valueObjIn != null) ? Mapper(valueObjIn, type) : null;
                                    try { SetValue(objOut, propInfoIn.Name, valueOut); }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            //Dapper
            else
            {
                foreach (KeyValuePair<string, dynamic> prop in objIn)
                {
                    try
                    {
                        if (PropExists(objOut, prop.Key))
                        {
                            var property = prop.Key;
                            var value = prop.Value;
                            var type = (prop.Value != null) ? prop.Value.GetType() : null;

                            if (type != null && type.FullName == "System.Collections.Generic")
                            {
                                var valueObjIn = value;
                                var t = GetPropertyTypeList(objOut, property);
                                if (valueObjIn != null && (valueObjIn as IEnumerable<object>).Count() > 0)
                                {
                                    var valueOut = MapperList(valueObjIn, t);
                                    try { SetValue(objOut, property, valueOut); }
                                    catch { }
                                }
                                else
                                    try { SetValue(objOut, property, CreateInstanceList(t)); }
                                    catch { }
                            }
                            else
                            {
                                if (IsNativeType(type) || (type != null && type.BaseType == typeof(Enum)))
                                {
                                    try { SetValue(objOut, property, value); }
                                    catch { }
                                }
                                else if (type != null)
                                {
                                    var valueOut = (value != null) ? Mapper(value, type) : null;
                                    try { SetValue(objOut, property, valueOut); }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
            return objOut;
        }

        private static IList CreateInstanceList(Type t)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t));
        }

        private static IDictionary CreateInstanceDictionary(Type t)
        {
            var types = t.GetGenericArguments();
            var keyType = types[0];
            var valueType = types[1];

            return (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(types));
        }

        private static bool IsNativeType(Type type)
        {
            var listNativeType = new List<Type>()
            {
                typeof(string), typeof(bool), typeof(bool?), typeof(long), typeof(long?),
                typeof(int), typeof(int?), typeof(Int32), typeof(Int32?), typeof(Int64), typeof(Int64?),
                typeof(double), typeof(double?), typeof(float), typeof(float?),
                typeof(DateTime), typeof(DateTime?), typeof(TimeSpan), typeof(TimeSpan?), typeof(Enum),
                typeof(byte), typeof(byte?), typeof(byte[])
            };

            return listNativeType.Any(x => x.Equals(type));
        }

        private static void SetValue(object obj, string property, object value)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            var type = propInfo.PropertyType;
            if (value != null && !propInfo.PropertyType.FullName.Contains(value.GetType().FullName))
                propInfo.SetValue(obj, Convert.ChangeType(value, propInfo.PropertyType), null);
            else
                propInfo.SetValue(obj, value, null);
        }

        private static object GetValue(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            return propInfo.GetValue(obj, null);
        }

        private static Type GetPropertyType(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            return propInfo.PropertyType;
        }

        private static Type GetPropertyTypeList(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            if (!string.IsNullOrWhiteSpace(propInfo.PropertyType.UnderlyingSystemType.FullName))
            {
                var fullName = propInfo.PropertyType.UnderlyingSystemType.FullName;

                fullName = fullName.Substring(0, fullName.LastIndexOf("]]"));
                fullName = fullName.Substring(fullName.IndexOf("[[") + 2);

                return Type.GetType(fullName);
            }
            else
                return propInfo.GetType();
        }

        private static Type GetPropertyTypeDictionary(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            if (!string.IsNullOrWhiteSpace(propInfo.PropertyType.UnderlyingSystemType.FullName))
            {
                var fullName = propInfo.PropertyType.UnderlyingSystemType.FullName;
                return Type.GetType(fullName);
            }
            else
                return propInfo.GetType();
        }

        private static Type GetTypeKeyDictionary(object obj)
        {
            var typing = obj.GetType().GetGenericArguments();
            var keyType = typing[0];
            return keyType;
        }

        private static Type GetTypeValueDictionary(object obj)
        {
            var typing = obj.GetType().GetGenericArguments();
            var valueType = typing[1];
            return valueType;
        }

        private static bool PropExists(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            return propInfo != null;
        }
    }
}
