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

namespace Recepcao.Aplicacao
{
    public class DynamicToObj
    {
        public static T MapperObject<T>(dynamic objIn)
        {
            var objOut = (T)Activator.CreateInstance(typeof(T));
            if (objIn != null)
            {
                //Dynamic
                if (!(objIn is IDynamicMetaObjectProvider))
                {
                    foreach (PropertyInfo propInfoIn in objIn.GetType().GetProperties())
                    {
                        if (propInfoIn.PropertyType.Namespace == "System.Collections.Generic")
                        {
                            var valueObjIn = propInfoIn.GetValue(objIn, null);
                            if (valueObjIn != null && valueObjIn.Count > 0 && PropExists(objOut, propInfoIn.Name))
                            {
                                var type = GetPropertyTypeList(objOut, propInfoIn.Name);
                                var valueOut = MapperObjectList(valueObjIn, type);
                                try { SetValue(objOut, propInfoIn.Name, valueOut); }
                                catch { }
                            }
                        }
                        else if (PropExists(objOut, propInfoIn.Name))
                        {
                            try { SetValue(objOut, propInfoIn.Name, propInfoIn.GetValue(objIn, null)); }
                            catch { }
                        }
                    }
                }
                //Dapper
                else
                {
                    foreach (var prop in objIn)
                    {
                        var property = prop.Key;
                        var value = prop.Value;
                        var type = (prop.Value != null) ? prop.Value.GetType() : null;

                        if (type != null && type.FullName == "System.Collections.Generic")
                        {
                            var valueObjIn = value;
                            if (valueObjIn != null && valueObjIn.Count > 0 && PropExists(objOut, property))
                            {
                                var t = GetPropertyTypeList(objOut, property);
                                var valueOut = MapperObjectList(valueObjIn, t);
                                try { SetValue(objOut, property, valueOut); }
                                catch { }
                            }
                        }
                        else if (PropExists(objOut, property))
                        {
                            try { SetValue(objOut, property, value); }
                            catch { }
                        }
                    }
                }
            }
            return objOut;
        }

        public static object MapperObjectList(dynamic objListIn, Type tOut)
        {
            var listOut = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tOut));
            foreach (var objIn in objListIn)
            {
                var objOut = Activator.CreateInstance(tOut);
                if (objIn != null)
                {
                    //Dynamic
                    if (!(objIn is IDynamicMetaObjectProvider))
                    {
                        foreach (var propInfoIn in objIn.GetType().GetProperties())
                        {
                            if (propInfoIn.PropertyType.Namespace == "System.Collections.Generic")
                            {
                                var value = propInfoIn.GetValue(objIn, null);
                                if (value != null && value.Count > 0 && PropExists(objOut, propInfoIn.Name))
                                {
                                    var type = GetPropertyTypeList(objOut, propInfoIn.Name);
                                    var valueOut = MapperObjectList(value, type);
                                    try { SetValue(objOut, propInfoIn.Name, valueOut); }
                                    catch { }
                                }
                            }
                            else if (PropExists(objOut, propInfoIn.Name))
                            {
                                try { SetValue(objOut, propInfoIn.Name, propInfoIn.GetValue(objIn, null)); }
                                catch { }
                            }
                        }
                    }
                    //Dapper
                    else
                    {
                        foreach (var prop in objIn)
                        {
                            if(prop != null)
                            {
                                var property = prop.Key;
                                var value = prop.Value;
                                var type = (prop.Value != null) ? prop.Value.GetType() : null;

                                if (type != null && type.FullName == "System.Collections.Generic")
                                {
                                    var valueObjIn = value;
                                    if (valueObjIn != null && valueObjIn.Count > 0 && PropExists(objOut, property))
                                    {
                                        var t = GetPropertyTypeList(objOut, property);
                                        var valueOut = MapperObjectList(valueObjIn, t);
                                        try { SetValue(objOut, property, valueOut); }
                                        catch { }
                                    }
                                }
                                else if (PropExists(objOut, property))
                                {
                                    try { SetValue(objOut, property, value); }
                                    catch { }
                                }
                            }
                        }
                    }
                    listOut.Add(objOut);
                }
            }
            return listOut;
        }

        public static void SetValue(object obj, string property, object value)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            var type = propInfo.PropertyType;
            if(value != null && !propInfo.PropertyType.FullName.Contains(value.GetType().FullName))
                propInfo.SetValue(obj, Convert.ChangeType(value, propInfo.PropertyType), null);
            else
                propInfo.SetValue(obj, value, null);
        }

        public static object GetValue(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            return propInfo.GetValue(obj, null);
        }

        public static Type GetPropertyType(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            return propInfo.GetType();
        }

        public static Type GetPropertyTypeList(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            if (!string.IsNullOrWhiteSpace(propInfo.PropertyType.UnderlyingSystemType.FullName))
            {
                var fullName = propInfo.PropertyType.UnderlyingSystemType.FullName;
                if (fullName.Contains(",")) fullName = fullName.Split(',').FirstOrDefault();
                fullName = fullName.Replace("System.Collections.Generic.List`1[[", "").Replace("]]", "");
                return Type.GetType(fullName);
            }
            else
                return propInfo.GetType();
        }

        public static bool PropExists(object obj, string property)
        {
            var propInfo = obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);
            return propInfo != null;
        }
    }
}
