using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{
	abstract class RedisSerializer
	{
        public RedisSerializer() { }
        public abstract Task<object> ReadAsync(RedisConvert convert, Type objectType, object existingValue, Namespace name);
        public abstract Task WriteAsync(RedisConvert convert, Type objectType, object value, Namespace name, TimeSpan? TTL = null);
    }
}
