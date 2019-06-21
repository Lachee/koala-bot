using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Redis.Serialize
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class RedisPropertyAttribute : Attribute
	{
        /// <summary>
        /// The name to display
        /// </summary>
		public string DisplayName { get; set; }

        /// <summary>
        /// The custom serializer to use
        /// </summary>
        public Type Serializer { get; set; }

        public RedisPropertyAttribute() : this(null) { }
		public RedisPropertyAttribute(string name = null, Type serializer = null)
		{
			this.DisplayName = name;
            this.Serializer = null;
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class RedisIgnoreAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class RedisOptionAttribute : Attribute
	{
        /// <summary>
        /// Should the value be serialized as a single key.
        /// </summary>
        public bool SingleValueKey { get; set; }

        /// <summary>
        /// The custom serializer to use
        /// </summary>
        public Type Serializer { get; set; }

        public RedisOptionAttribute() { SingleValueKey = false; }
		public RedisOptionAttribute( bool serializeAsKey) { SingleValueKey = serializeAsKey; }
	}
}
