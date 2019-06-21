using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{

    class HashSetSerializer : RedisSerializer
    {
        public override async Task<object> ReadAsync(RedisConvert convert, Type objectType, object existingValue, Namespace name)
        {
            //Make sure the type is valid.
            var types = objectType.GetGenericArguments();
            Debug.Assert(types.Length == 1);

            if (!types[0].IsPrimitive && !types[0].IsEnum)
                throw new ArgumentException("HashSet type is invalid. Only primitive HashSets are supported.", "value");

            //Get the hashset
            HashSet<string> hashset = await convert.Client.FetchHashSetAsync(name.Build());

            //Prepare a hashset of the new type
            var existingSet = existingValue;
            if (existingValue == null) existingSet = Activator.CreateInstance(objectType);

            //Prepare the add function
            var add = objectType.GetMethod("Add");
            
            //Iterate over every value in the hashset adding it to the new hashset
            foreach(var s in hashset)
            {
                object val = TypeDescriptor.GetConverter(types[0]).ConvertFromString(s);
                add.Invoke(existingSet, new object[] { val });
            }

            return existingSet;
        }

        public override async Task WriteAsync(RedisConvert convert, Type objectType, object value, Namespace name, TimeSpan? TTL = null)
        {
            //Prepare the hashset we will copy into
            HashSet<string> hashset = null;
            if (objectType == typeof(HashSet<string>))
            {
                hashset = (HashSet<string>)value;
            }
            else
            {
                var types = objectType.GetGenericArguments();
                Debug.Assert(types.Length == 1);

                //Make sure the type is valid.
                if (!types[0].IsPrimitive && !types[0].IsEnum)
                    throw new ArgumentException("HashSet type is invalid. Only primitive HashSets are supported.", "value");

                //Prepare the set
                hashset = new HashSet<string>();

                //Fetch the enumerator
                var getEnumerator = objectType.GetMethod("GetEnumerator");
                var enumerator = (IEnumerator)getEnumerator.Invoke(value, null);

                //Iterate over every element converting them to a string and adding them to our set
                while (enumerator.MoveNext())
                {
                    hashset.Add(enumerator.Current.ToString());
                }
            }

            //Write to the DB
            await convert.WriteHashSetAsync(name, hashset, TTL, true);
        }
    }

}
