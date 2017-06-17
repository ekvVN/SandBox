namespace Common.Log
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    [ExcludeFromCodeCoverage]
    public static class TypeExtensions
    {
        public static ConstructorInfo GetConstructorPortable(this Type type, params Type[] types)
        {
            return type.GetConstructor(types);
        }

        public static MethodInfo GetMethodPortable(this Type type, string name)
        {
            return type.GetMethod(name);
        }

        public static MethodInfo GetMethodPortable(this Type type, string name, params Type[] types)
        {
            return type.GetMethod(name, types);
        }

        public static PropertyInfo GetPropertyPortable(this Type type, string name)
        {
            return type.GetProperty(name);
        }

        public static IEnumerable<FieldInfo> GetFieldsPortable(this Type type)
        {
            return type.GetFields();
        }

        public static Type GetBaseTypePortable(this Type type)
        {
            return type.BaseType;
        }

        public static object CreateDelegate(this MethodInfo methodInfo, Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, methodInfo);
        }

        public static Assembly GetAssemblyPortable(this Type type)
        {
            return type.Assembly;
        }
    }
}