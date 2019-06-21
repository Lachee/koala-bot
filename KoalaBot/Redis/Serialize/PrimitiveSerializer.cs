using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{
    class PrimitiveSerializer : RedisSerializer
    {
        public override async Task<object> ReadAsync(RedisConvert convert, Type objectType, object existingValue, Namespace name)
        {
            //Get the current string value
            string value = await convert.Client.FetchStringAsync(name.Build(), null);
            if (value == null) return existingValue;

            //If we are a primitive or enum then convert it from string and return the value
            if (objectType.IsPrimitive || objectType.IsEnum)
                return TypeDescriptor.GetConverter(objectType).ConvertFromString(value);

            //Return the raw string value.
            return value;
        }

        public override async Task WriteAsync(RedisConvert convert, Type objectType, object value, Namespace name, TimeSpan? TTL = null)
        {
            //Convert to string and then write it to the converter
            await convert.WriteValueAsync(name, value.ToString(), TTL);
            return;
        }
    }
}
