using System;

namespace FeiniuBus.Restful.Text
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializablePropertyAttribute : Attribute
    {
        public SerializablePropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; set; }
    }
}
