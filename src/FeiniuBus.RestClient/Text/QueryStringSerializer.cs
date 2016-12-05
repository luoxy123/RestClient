using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FeiniuBus.RestClient.Text
{
    public class QueryStringSerializer
    {
        public static string SerializeObject(object value)
        {
            return SerializeObject(value, new QueryStringTypeSerializerSettings());
        }

        public static string SerializeObject(object value, QueryStringTypeSerializerSettings settings)
        {
            var type = value.GetType();
            if (type.IsAssignableFrom(typeof(IDictionary)) || type.HasInterface(typeof(IDictionary)))
                using (var writer = new StringWriter())
                {
                    WriteIDictionary(writer, (IDictionary)value, settings);
                    return writer.ToString();
                }

            var isEnumerable = type.IsAssignableFrom(typeof(IEnumerable)) || type.HasInterface(typeof(IEnumerable));
            if (isEnumerable)
                throw new NotSupportedException("不支持可枚举类型的序列化");
            if (type.GetTypeInfo().IsClass || type.GetTypeInfo().IsInterface)
                using (var writer = new StringWriter())
                {
                    WriteType(writer, value, settings);
                    return writer.ToString();
                }

            throw new NotSupportedException("不支持的对象类型");
        }

        private static void WriteType(TextWriter writer, object value, QueryStringTypeSerializerSettings settings)
        {
            var type = value.GetType();
            var props = type.GetSerializableProperties();
            var serializer = new QueryStringTypeSerializer(settings);

            for (var i = 0; i < props.Length; i++)
            {
                var pi = props[i];
                var pValue = pi.GetValue(value);
                if (pValue == null) continue;

                if (i > 0)
                    writer.Write('&');

                writer.Write(GetPropertyName(pi));
                writer.Write("=");

                WriteValue(serializer, writer, pValue);
            }
        }

        private static string GetPropertyName(PropertyInfo pi)
        {
            var attr = pi.AllAttributes().FirstOrDefault(x => x.GetType() == typeof(SerializablePropertyAttribute));
            if (attr == null)
                return pi.Name.ToLowercaseUnderscore();

            var ins = (SerializablePropertyAttribute)attr;
            return ins.PropertyName;
        }

        private static void WriteIDictionary(TextWriter writer, IDictionary map,
            QueryStringTypeSerializerSettings settings)
        {
            var serializer = new QueryStringTypeSerializer(settings);
            var ranOnce = false;

            foreach (var key in map.Keys)
            {
                var dictionaryValue = map[key];
                if (dictionaryValue == null)
                    continue;

                if (ranOnce)
                    writer.Write("&");
                else
                    ranOnce = true;

                WriteValue(serializer, writer, key);
                writer.Write("=");
                WriteValue(serializer, writer, dictionaryValue);
            }
        }

        private static void WriteValue(ITypeSerializer serializer, TextWriter writer, object value)
        {
            var type = value.GetType();
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsValueType)
            {
                WriteValueTypeToString(serializer, writer, value);
                return;
            }

            if (typeInfo.IsArray)
            {
                var elementType = type.GetElementType();
                if (!elementType.GetTypeInfo().IsValueType)
                    throw new NotSupportedException("数组的类型包含了非值类型，当前不受支持");

                if (type == typeof(byte[]))
                    serializer.WriteBytes(writer, value);
                else if (type == typeof(string[]))
                    WriteStringArray(serializer, writer, value);
                else
                    WriteGenericArrayValueType(serializer, writer, value);
                return;
            }

            serializer.WriteBuiltIn(writer, value);
        }

        private static void WriteGenericArrayValueType(ITypeSerializer serializer, TextWriter writer, object array)
        {
            var enumerable = array as IEnumerable;
            if (enumerable != null)
            {
                writer.Write('[');
                var ranOnce = false;

                foreach (var item in enumerable)
                {
                    WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                    WriteValueTypeToString(serializer, writer, item);
                }

                writer.Write(']');
            }
        }

        private static void WriteValueTypeToString(ITypeSerializer serializer, TextWriter writer, object value)
        {
            var type = value.GetType();
            var underlyingType = Nullable.GetUnderlyingType(type);
            var isNullable = underlyingType != null;
            if (underlyingType == null)
                underlyingType = type;

            if (!underlyingType.GetTypeInfo().IsEnum)
            {
                var typeCode = underlyingType.GetTypeCode();

                if (typeCode == TypeCode.Char)
                    serializer.WriteChar(writer, value);
                else if (typeCode == TypeCode.Int32)
                    serializer.WriteInt32(writer, value);
                else if (typeCode == TypeCode.Int64)
                    serializer.WriteInt64(writer, value);
                else if (typeCode == TypeCode.UInt64)
                    serializer.WriteUInt64(writer, value);
                else if (typeCode == TypeCode.UInt32)
                    serializer.WriteUInt32(writer, value);
                else if (typeCode == TypeCode.Byte)
                    serializer.WriteByte(writer, value);
                else if (typeCode == TypeCode.SByte)
                    serializer.WriteSByte(writer, value);
                else if (typeCode == TypeCode.Int16)
                    serializer.WriteInt16(writer, value);
                else if (typeCode == TypeCode.UInt16)
                    serializer.WriteUInt16(writer, value);
                else if (typeCode == TypeCode.Boolean)
                    serializer.WriteBool(writer, value);
                else if (typeCode == TypeCode.Single)
                    serializer.WriteFloat(writer, value);
                else if (typeCode == TypeCode.Double)
                    serializer.WriteDouble(writer, value);
                else if (typeCode == TypeCode.Decimal)
                    serializer.WriteDecimal(writer, value);
                else if (typeCode == TypeCode.DateTime)
                    if (isNullable)
                        serializer.WriteNullableDateTime(writer, value);
                    else
                        serializer.WriteDateTime(writer, value);
                else if (type == typeof(DateTimeOffset))
                    serializer.WriteDateTimeOffset(writer, value);
                else if (type == typeof(DateTimeOffset?))
                    serializer.WriteNullableDateTimeOffset(writer, value);
                else if (type == typeof(TimeSpan))
                    serializer.WriteTimeSpan(writer, value);
                else if (type == typeof(TimeSpan?))
                    serializer.WriteNullableTimeSpan(writer, value);
                else if (type == typeof(System.Guid))
                    serializer.WriteGuid(writer, value);
                else if (type == typeof(System.Guid?))
                    serializer.WriteNullableGuid(writer, value);
            }
            else
            {
                if (type.FirstAttribute<FlagsAttribute>() != null)
                    serializer.WriteEnumFlags(writer, value);
                else
                    serializer.WriteEnum(writer, value);
            }
        }

        private static void WriteStringArray(ITypeSerializer serializer, TextWriter writer, object sarray)
        {
            writer.Write('[');

            var list = (string[])sarray;
            var ranOnce = false;
            var listLength = list.Length;

            for (var i = 0; i < listLength; i++)
            {
                WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                serializer.WriteString(writer, list[i]);
            }

            writer.Write(']');
        }

        private static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
        {
            if (ranOnce)
                writer.Write(',');
            else
                ranOnce = true;
        }
    }
}
