using System;
using System.Globalization;
using System.IO;

namespace FeiniuBus.Restful.Text
{
    internal class QueryStringTypeSerializer : ITypeSerializer
    {
        public QueryStringTypeSerializer(QueryStringTypeSerializerSettings settings)
        {
            Settings = settings;
        }

        internal QueryStringTypeSerializerSettings Settings { get; }

        public bool IncludeNullValues => false;

        public void WriteString(TextWriter writer, string value)
        {
            writer.Write(string.IsNullOrEmpty(value) ? "\"\"" : value.UrlEncode());
        }

        public void WriteBuiltIn(TextWriter writer, object value)
        {
            writer.Write(value);
        }

        public void WriteDateTime(TextWriter writer, object oDateTime)
        {
            var dateTime = (DateTime)oDateTime;
            if (Settings.DateHandler == DateHandler.Number)
            {
                writer.Write(dateTime.ToUnixTime());
                return;
            }

            writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString(dateTime));
        }

        public void WriteNullableDateTime(TextWriter writer, object dateTime)
        {
            if (dateTime == null)
                return;

            WriteDateTime(writer, dateTime);
        }

        public void WriteDateTimeOffset(TextWriter writer, object oDateTimeOffset)
        {
            writer.Write(((DateTimeOffset)oDateTimeOffset).ToString("o"));
        }

        public void WriteNullableDateTimeOffset(TextWriter writer, object dateTimeOffset)
        {
            if (dateTimeOffset == null)
                return;

            WriteDateTimeOffset(writer, dateTimeOffset);
        }

        public void WriteTimeSpan(TextWriter writer, object oTimeSpan)
        {
            writer.Write(DateTimeSerializer.ToXsdTimeSpanString((TimeSpan)oTimeSpan));
        }

        public void WriteNullableTimeSpan(TextWriter writer, object timeSpan)
        {
            if (timeSpan == null)
                return;

            WriteTimeSpan(writer, timeSpan);
        }

        public void WriteGuid(TextWriter writer, object oValue)
        {
            writer.Write(((Guid)oValue).ToString("n"));
        }

        public void WriteNullableGuid(TextWriter writer, object oValue)
        {
            if (oValue == null)
                return;
            WriteGuid(writer, oValue);
        }

        public void WriteBytes(TextWriter writer, object oByteValue)
        {
            if (oByteValue == null)
                return;
            writer.Write(Convert.ToBase64String((byte[])oByteValue));
        }

        public void WriteChar(TextWriter writer, object charValue)
        {
            if (charValue == null)
                return;
            writer.Write((char)charValue);
        }

        public void WriteByte(TextWriter writer, object byteValue)
        {
            if (byteValue == null)
                return;
            writer.Write((byte)byteValue);
        }

        public void WriteSByte(TextWriter writer, object sbyteValue)
        {
            if (sbyteValue == null)
                return;
            writer.Write((sbyte)sbyteValue);
        }

        public void WriteInt16(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((short)intValue);
        }

        public void WriteUInt16(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((ushort)intValue);
        }

        public void WriteInt32(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((int)intValue);
        }

        public void WriteUInt32(TextWriter writer, object uintValue)
        {
            if (uintValue == null)
                return;
            writer.Write((uint)uintValue);
        }

        public void WriteInt64(TextWriter writer, object longValue)
        {
            if (longValue == null)
                return;
            writer.Write((long)longValue);
        }

        public void WriteUInt64(TextWriter writer, object ulongValue)
        {
            if (ulongValue == null)
                return;
            writer.Write((ulong)ulongValue);
        }

        public void WriteBool(TextWriter writer, object boolValue)
        {
            if (boolValue == null)
                return;
            writer.Write((bool)boolValue);
        }

        public void WriteFloat(TextWriter writer, object floatValue)
        {
            if (floatValue == null)
                return;
            var floatVal = (float)floatValue;
            if (Equals(floatVal, float.MaxValue) || Equals(floatVal, float.MinValue))
                writer.Write(floatVal.ToString("r"));
            else
                writer.Write(floatVal.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteDouble(TextWriter writer, object doubleValue)
        {
            if (doubleValue == null)
                return;
            var doubleVal = (double)doubleValue;

            if (Equals(doubleVal, double.MaxValue) || Equals(doubleVal, double.MaxValue))
                writer.Write(doubleVal.ToString("r"));
            else
                writer.Write(doubleVal.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteDecimal(TextWriter writer, object decimalValue)
        {
            if (decimalValue == null)
                return;

            writer.Write(((decimal)decimalValue).ToString(CultureInfo.InvariantCulture));
        }

        public void WriteEnum(TextWriter writer, object enumValue)
        {
            if (enumValue == null)
                return;
            if (Settings.TreatEnumAsInteger)
                WriteEnumFlags(writer, enumValue);
            else
                writer.Write(enumValue.ToString());
        }

        public void WriteEnumFlags(TextWriter writer, object enumFlagValue)
        {
            if (enumFlagValue == null)
                return;

            var typeCode = Enum.GetUnderlyingType(enumFlagValue.GetType()).GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.SByte:
                    writer.Write((sbyte)enumFlagValue);
                    break;
                case TypeCode.Byte:
                    writer.Write((byte)enumFlagValue);
                    break;
                case TypeCode.Int16:
                    writer.Write((short)enumFlagValue);
                    break;
                case TypeCode.UInt16:
                    writer.Write((ushort)enumFlagValue);
                    break;
                case TypeCode.Int32:
                    writer.Write((int)enumFlagValue);
                    break;
                case TypeCode.UInt32:
                    writer.Write((uint)enumFlagValue);
                    break;
                case TypeCode.Int64:
                    writer.Write((long)enumFlagValue);
                    break;
                case TypeCode.UInt64:
                    writer.Write((ulong)enumFlagValue);
                    break;
                default:
                    writer.Write((int)enumFlagValue);
                    break;
            }
        }
    }
}
