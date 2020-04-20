using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.Common
{
    public static class ReflectionUtility
    {
        public static PropertyInfo[] GetProperties(this Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            while (type != null)
            {
                var typeInfo = type.GetTypeInfo();
                properties.AddRange(typeInfo.DeclaredProperties);
                type = typeInfo.BaseType;
            }

            return properties.ToArray();
        }

        public static MethodInfo GetSetMethod(this PropertyInfo propertyInfo)
        {
            return propertyInfo.SetMethod;
        }

        public static bool IsAssignableFrom(this Type thisType, Type type)
        {
            return thisType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        public static MethodInfo GetMethod(this Type type, string name)
        {
            while (type != null)
            {
                var typeInfo = type.GetTypeInfo();
                var method = typeInfo.GetDeclaredMethod(name);
                if (method != null)
                    return method;
                type = typeInfo.BaseType;
            }

            return null;
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static Type[] GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.ToArray();
        }
    }
}
