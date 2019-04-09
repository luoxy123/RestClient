using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FeiniuBus.Restful.Text
{
    public static class ReflectionExtensions
    {
        private static readonly Dictionary<Type, TypeCode> TypeCodeTable =
            new Dictionary<Type, TypeCode>
            {
                {typeof(bool), TypeCode.Boolean},
                {typeof(char), TypeCode.Char},
                {typeof(byte), TypeCode.Byte},
                {typeof(short), TypeCode.Int16},
                {typeof(int), TypeCode.Int32},
                {typeof(long), TypeCode.Int64},
                {typeof(sbyte), TypeCode.SByte},
                {typeof(ushort), TypeCode.UInt16},
                {typeof(uint), TypeCode.UInt32},
                {typeof(ulong), TypeCode.UInt64},
                {typeof(float), TypeCode.Single},
                {typeof(double), TypeCode.Double},
                {typeof(DateTime), TypeCode.DateTime},
                {typeof(decimal), TypeCode.Decimal},
                {typeof(string), TypeCode.String}
            };

        public static TypeCode GetTypeCode(this Type type)
        {
            if (type == null)
                return TypeCode.Empty;

            TypeCode result;
            if (!TypeCodeTable.TryGetValue(type, out result))
                result = TypeCode.Object;

            return result;
        }

        public static bool HasInterface(this Type type, Type interfaceType)
        {
            foreach (var t in type.GetTypeInterfaces())
                if (t == interfaceType)
                    return true;

            return false;
        }

        public static Type[] GetTypeInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.ToArray();
        }

        public static TAttr FirstAttribute<TAttr>(this Type type) where TAttr : class
        {
            return type.GetTypeInfo().GetCustomAttributes(typeof(TAttr), true).Cast<TAttr>().FirstOrDefault();
        }

        internal static PropertyInfo[] GetSerializableProperties(this Type type)
        {
            return type.GetPublicProperties().OnlySerializableProperties();
        }

        internal static PropertyInfo[] GetTypesPublicProperties(this Type type)
        {
            var pis = new List<PropertyInfo>();
            foreach (var pi in type.GetRuntimeProperties())
            {
                var mi = pi.GetMethod ?? pi.SetMethod;
                if (mi != null && mi.IsStatic) continue;

                pis.Add(pi);
            }

            return pis.ToArray();
        }

        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();
                var considered = new List<Type>();
                var queue = new Queue<Type>();

                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetTypeInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetTypesPublicProperties();

                    var newPropertyInfos = typeProperties.Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetTypesPublicProperties().Where(t => t.GetIndexParameters().Length == 0).ToArray();
        }

        internal static PropertyInfo[] OnlySerializableProperties(this PropertyInfo[] properties)
        {
            var readableProperties = properties.Where(x => x.PropertyGetMethod() != null);

            return
                readableProperties.Where(
                    prop => prop.AllAttributes().All(attr => attr.GetType() != typeof(IgnoreMemberAttribute))).ToArray();
        }

        internal static MethodInfo PropertyGetMethod(this PropertyInfo pi, bool nonPublic = false)
        {
            var mi = pi.GetMethod;
            return mi != null && (nonPublic || mi.IsPublic) ? mi : null;
        }

        public static Attribute[] AllAttributes(this PropertyInfo propertyInfo)
        {
            return (Attribute[]) propertyInfo.GetCustomAttributes(true).ToArray();
        }
    }
}
