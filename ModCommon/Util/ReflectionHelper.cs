using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using MonoMod.Utils;

namespace ModCommon.Util
{
    public static class ReflectionHelper
    {
        private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> METHOD_INFOS =
            new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public static MethodInfo GetMethodInfo(object obj, string name, bool instance = true)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return null;

            Type t = obj.GetType();

            if (!METHOD_INFOS.ContainsKey(t))
            {
                METHOD_INFOS.Add(t, new Dictionary<string, MethodInfo>());
            }

            Dictionary<string, MethodInfo> typeInfos = METHOD_INFOS[t];

            if (!typeInfos.ContainsKey(name))
            {
                typeInfos.Add(name,
                    t.GetMethod(name,
                        BindingFlags.NonPublic | BindingFlags.Public |
                        (instance ? BindingFlags.Instance : BindingFlags.Static)));
            }

            return typeInfos[name];
        }

        public static T InvokeMethod<T>(object obj, string name, bool instance = true, params object[] args)
        {
            return (T) obj.GetMethodInfo(name, instance).GetFastDelegate().Invoke(obj, args);
        }
    }

    
    public static class ReflectionExtensions
    {
        [Obsolete("Use SetAttr<TObject, TField>")]
        public static void SetAttr<T>(this object obj, string name, T val, bool instance = true) =>
            Modding.ReflectionHelper.SetAttr(obj, name, val, instance);

        [Obsolete("Use GetAttr<TType, TField>")]
        public static T GetAttr<T>(this object obj, string name, bool instance = true) =>
            Modding.ReflectionHelper.GetAttr<T>(obj, name, instance);
        
        [PublicAPI]
        public static TField GetAttr<TObject, TField>(this TObject obj, string name) =>
            Modding.ReflectionHelper.GetAttr<TObject, TField>(obj, name);
        
        [PublicAPI]
        public static void SetAttr<TObject, TField>(this TObject obj, string name, TField val) =>
            Modding.ReflectionHelper.SetAttr(obj, name, val);

        [PublicAPI]
        public static MethodInfo GetMethodInfo(this object obj, string name, bool instance = true) =>
            ReflectionHelper.GetMethodInfo(obj, name, instance);
        
        [PublicAPI]
        public static object InvokeMethod<T>(this object obj, string name, bool instance = true, params object[] args) => 
            ReflectionHelper.InvokeMethod<T>(obj, name, instance, args);
    }
}