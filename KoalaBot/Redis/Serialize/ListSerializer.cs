using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{

    class ListSerializer : RedisSerializer
    {
        public override Task<object> ReadAsync(RedisConvert convert, Type objectType, object existingValue, Namespace name)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(RedisConvert convert, Type objectType, object value, Namespace name, TimeSpan? TTL = null)
        {
            throw new NotImplementedException();
        }
    }


}
