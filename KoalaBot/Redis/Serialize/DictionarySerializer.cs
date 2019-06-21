using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{

    class DictionarySerializer : RedisSerializer
    {
        public override Task<object> ReadAsync(RedisConvert convert, Type objectType, object existingValue, Namespace name)
        {
            throw new NotImplementedException();
        }

        public override async Task WriteAsync(RedisConvert convert, Type objectType, object value, Namespace name, TimeSpan? TTL = null)
        {
            //Prepare the map. If we are already the correct type just use the raw value, 
            // otherwise we will convert it to the correct type.
            Dictionary<string, string> map = null;
            if (objectType == typeof(Dictionary<string, string>))
            {
                map = (Dictionary<string, string>)value;
            }
            else
            {

                //Get the generic types and make sure they are valid
                var genericTypes = objectType.GetGenericArguments();
                Debug.Assert(genericTypes.Length == 2);

                if ((!genericTypes[0].IsPrimitive && !genericTypes[0].IsEnum) || (!genericTypes[1].IsPrimitive && !genericTypes[1].IsEnum))
                    throw new ArgumentException("Dictionary type is invalid. Only primitive Dictionaries are supported.", "value");

                //Get the enumerator for the dictionary ( value.GetEnumerator(); )
                var getEnumerator = objectType.GetMethod("GetEnumerator");
                var enumerator = (IEnumerator)getEnumerator.Invoke(value, null);
                
                //Get the properties of the keypairs
                var keypairType = typeof(KeyValuePair<,>).MakeGenericType(genericTypes);
                var keyProperty = keypairType.GetProperty("Key");
                var valueProperty = keypairType.GetProperty("Value");

                //Iterate over every item and add them to the map
                while (enumerator.MoveNext())
                {
                    var kp_key = keyProperty.GetValue(enumerator.Current);
                    var kp_value = valueProperty.GetValue(enumerator.Current);
                    map[kp_key.ToString()] = kp_value.ToString();
                }
            }

            //Store the map in Redis
            await convert.WriteHashMapAsync(name, map, TTL);
        }
    }
}
