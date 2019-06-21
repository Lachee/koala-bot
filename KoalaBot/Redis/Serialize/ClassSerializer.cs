using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{
    class ClassSerializer : RedisSerializer
    {
        public override Task<object> ReadAsync(RedisConvert convert, Type objectType, object existingValue, Namespace name)
        {
            throw new NotImplementedException();
        }

        public override async Task WriteAsync(RedisConvert convert, Type objectType, object value, Namespace name, TimeSpan? TTL = null)
        {
            //Prepare the options
            RedisOptionAttribute options = objectType.GetCustomAttribute<RedisOptionAttribute>();
            if (options == null) options = new RedisOptionAttribute();

            //Store it as a simple string
            if (options.SingleValueKey)
            {
                await convert.Client.StoreStringAsync(name.Build(), value.ToString(), TTL);
                return;
            }

            //The content that will be written to the HashMap in bulk
            Dictionary<string, string> hashmap = new Dictionary<string, string>();

            //Iterate over every property
            foreach (var property in objectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var ignoreAttribute = property.GetCustomAttribute<RedisIgnoreAttribute>();
                if (ignoreAttribute != null) continue;

                //Prepare some property values
                var propertyName = property.Name;
                var propertyAttribute = property.GetCustomAttribute<RedisPropertyAttribute>();
                if (propertyAttribute != null)
                {
                    propertyName = propertyAttribute.DisplayName ?? property.Name;
                }

                //Prepare the serializer
                var serializer = convert.GetSerializerForType(property.PropertyType, property: propertyAttribute);
                Debug.Assert(serializer != null);

                //Push the name and then serialize it
                name.Push(propertyName);
                convert.SetHashMapBuffer(hashmap);
                await serializer.WriteAsync(convert, property.PropertyType, property.GetValue(value), name, TTL);
                name.Pop();
            }

            //Flush our buffer
            convert.SetHashMapBuffer(hashmap);
            await convert.FlushHashMapAsync(name, TTL);
        }
    }
}
