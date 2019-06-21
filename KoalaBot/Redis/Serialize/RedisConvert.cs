using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace KoalaBot.Redis.Serialize
{
	class RedisConvert
	{
        public IRedisClient Client { get; }
        public IRedisTransaction Transaction { get; private set; }

        public RedisConvert(IRedisClient client)
        {
            this.Client = client;
            this.Transaction = null;
        }

        private Dictionary<string, string> _hashmapBuffer;

        public async Task<T> ReadAsync<T>(Namespace key)
        {
            ////Prepare the type
            //Type type = typeof(T);
            //
            ////Does it have a special deserializer? If not we will create a default serializer.
            //RedisSerializerAttribute serializerAttribute = type.GetCustomAttribute<RedisSerializerAttribute>();
            //RedisSerializer serializer = null;
            //
            //if (serializerAttribute == null || serializerAttribute.Serializer == null) serializer = new DefaultRedisSerializer();
            //else serializer = (RedisSerializer) Activator.CreateInstance(serializerAttribute.Serializer);
            //    
            //Debug.Assert(serializer != null);
            //
            //Use the serializer to read the data
            return default(T);
            //return (T) await serializer.ReadAsync(this, type, null);
        }
        
        /// <summary>
        /// Writes the object to the redis database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        /// <param name="TTL"></param>
        /// <returns></returns>
        public async Task WriteAsync(Namespace name, object obj, TimeSpan? TTL = null, bool useTransaction = true)
        {
            //Set the connection to a new transaction
            if (useTransaction)
                Transaction = Client.CreateTransaction();

            //Prepare the type
            Type type = obj.GetType();
            var serializer = GetSerializerForType(type, options: type.GetCustomAttribute<RedisOptionAttribute>());
            Debug.Assert(serializer != null);

            //Serialzie the content
            await serializer.WriteAsync(this, type, obj, name, TTL);

            //Execute the transaction and clear it
            if (useTransaction)
            {
                await Transaction.ExecuteAsync();
                Transaction = null;
            }
        }

        /// <summary>
        /// Gets the serializer for the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public RedisSerializer GetSerializerForType(Type type, RedisOptionAttribute options = null, RedisPropertyAttribute property = null)
        {
            if (options != null && options.Serializer != null)
                return (RedisSerializer)Activator.CreateInstance(options.Serializer);
            
            if (property != null && property.Serializer != null)
                return (RedisSerializer)Activator.CreateInstance(property.Serializer);

            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                return new PrimitiveSerializer();

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    return new DictionarySerializer();

                if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                    return new HashSetSerializer();

                if (type.GetGenericTypeDefinition() == typeof(List<>))
                    return new ListSerializer();
            }

            return new ClassSerializer();
        }

        /// <summary>
        /// Writes a string key pair to the redis. If there is a buffer available then it will write it to the hashmap buffer.
        /// </summary>
        /// <param name="name">The key to insert</param>
        /// <param name="value">The value to insert</param>
        /// <param name="TTL">The expiry timespan of the entry</param>
        /// <param name="skipBuffer">Skips the buffer and writes directly to the client.</param>
        /// <returns></returns>
        public async Task WriteValueAsync(Namespace name, string value, TimeSpan? TTL = null, bool skipBuffer = false)
        {
            if (_hashmapBuffer != null && !skipBuffer)
            {
                _hashmapBuffer[name.Peek()] = value;
                return;
            }

            var conn = Transaction ?? (IRedisConnection) Client;
            await conn.StoreStringAsync(name.Build(), value, TTL);
        }

        /// <summary>
        /// Writes a hashmap to the redis
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <param name="TTL"></param>
        /// <returns></returns>
        public async Task WriteHashMapAsync(Namespace name, Dictionary<string, string> values, TimeSpan? TTL = null)
        {
            var key = name.Build();

            var conn = Transaction ?? (IRedisConnection)Client;
            await conn.StoreHashMapAsync(key, values);
            if (TTL.HasValue) await conn.SetExpiryAsync(key, TTL.Value);
        }

        /// <summary>
        /// Writes and clears the value buffer.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="TTL"></param>
        /// <returns></returns>
        public async Task<bool> FlushHashMapAsync(Namespace name, TimeSpan? TTL = null)
        {
            if (_hashmapBuffer == null) return false;
            await WriteHashMapAsync(name, _hashmapBuffer, TTL);
            _hashmapBuffer = null;
            return true;
        }

        public void SetHashMapBuffer(Dictionary<string, string> buffer) { this._hashmapBuffer = buffer; }

        /// <summary>
        /// Writes a hashset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <param name="TTL"></param>
        /// <returns></returns>
        public async Task WriteHashSetAsync(Namespace name, HashSet<string> values, TimeSpan? TTL = null, bool clear = true)
        {
            var key = name.Build();
            var conn = Transaction ?? (IRedisConnection)Client;
            if (clear) await conn.RemoveAsync(key);
            await conn.AddHashSetAsync(key, values);
        }

        /// <summary>
        /// Writes a list
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="TTL"></param>
        /// <returns></returns>
        public async Task WriteListAsync(Namespace name, List<string> value, TimeSpan? TTL = null) { throw new NotImplementedException(); }
        public async Task WriteListAsync(Namespace name, IEnumerable<string> value, TimeSpan? TTL = null) => await WriteListAsync(name, value.ToList(), TTL);

        /// <summary>
        /// Deserializes a dictionary into a object
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="buffer">The data that was received by the redis</param>
        /// <returns></returns>
        public static T Deserialize<T>(Dictionary<string, string> buffer)
		{
            //Buffer is null, return default.
            if (buffer == null)
                return default(T);

			//Prepare the type
			Type type = typeof(T);
			var constructor = type.GetConstructor(new Type[0]);
            if (constructor == null) throw new MissingMethodException(type.FullName, "constructor()");

            //Create the reference and deserialize
			object reference = constructor.Invoke(new object[0]);

			//Deserialize, returning default if we find jack all
			if (!Deserialize(buffer, type, ref reference)) return default(T);

			//Return a cast
			return (T) reference;
		}

		/// <summary>
		/// Deserializes a dictionary into a object
		/// </summary>
		/// <param name="buffer">The data that was received by the redis</param>
		/// <param name="type">The type of target object</param>
		/// <param name="reference">The target object</param>
		/// <param name="subkey">The subkey (optional)</param>
		/// <returns></returns>
		private static bool Deserialize(Dictionary<string, string> buffer, Type type, ref object reference, string subkey = "")
        {
            Debug.Assert(type != null);
            Debug.Assert(reference != null);
            Debug.Assert(buffer != null);

            //Prepare the type and initial options
            bool hasElements = false;

			//See if we should override the options
			RedisOptionAttribute options = type.GetCustomAttribute<RedisOptionAttribute>();
            if (options == null) options = new RedisOptionAttribute();

			//Create a new instance of the type
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				if (DeserializeMember(buffer, type, new SerializeMember(property), ref reference, subkey, options))
					hasElements = true;

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				if (DeserializeMember(buffer, type, new SerializeMember(field), ref reference, subkey, options))
					hasElements = true;

			//Return the element state
			return hasElements;
		}

		private static bool DeserializeMember(Dictionary<string, string> buffer, Type type, SerializeMember member, ref object reference, string subkey, RedisOptionAttribute options)
		{
            //Ignore elements with ignore attributes
			var ignoreAttribute = member.GetCustomAttribute<RedisIgnoreAttribute>();
			if (ignoreAttribute != null) return false;

			//Ignore generated blocks
			var generatedAttribute = member.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
			if (generatedAttribute != null) return false;

            //Serialize the member if it has an attribute or is forced to serialize.
			var attribute = member.GetCustomAttribute<RedisPropertyAttribute>();
            if (attribute == null) attribute = new RedisPropertyAttribute(member.Name);

			//Prepare its key
			string key = PrepareKey(attribute, member.Name, subkey);

			//If class, we have to do something completely different
			//If it has a serialize attribute, we want to construct its serializer
			//var serializerAttribute = member.GetCustomAttribute<RedisSerializerAttribute>();
			//if (serializerAttribute != null)
			//{
			//	//They have a custom serializer, so lets construct its type
			//	var constructor = serializerAttribute.Serializer.GetConstructor(new Type[0]);
			//	if (constructor != null)
			//	{
			//		var serializer = constructor.Invoke(new object[0]) as RedisSerializer;
			//		if (serializer != null)
			//		{
			//			object v = serializer.Deserialize(buffer, member, key);
			//			member.SetValue(reference, v);
			//			return true;
			//		}
            //
			//		throw new Exception("Bad Serialization on the custom serializer! Failed to cast into a RedisSerializer");
			//	}
            //
			//	throw new Exception("Bad Serialization on the custom serializer! Failed to find a constructor with 0 elements");
			//}

			//If the property is a string, just cast ez pz
			if (member.IsPrimitive || member.IsString || member.IsEnum)
			{
				string primval;
				if (buffer.TryGetValue(key, out primval))
				{
					if (member.IsPrimitive || member.IsEnum)
					{
						object v = TypeDescriptor.GetConverter(member.Type).ConvertFromString(primval);
						member.SetValue(reference, v);
					}
					else
					{
						member.SetValue(reference, primval);
					}

					return true;
				}

				return false;
			}

			//We have to do it the classical way with a subkey
			//var propvalConstructor = propertyType.GetConstructor(new Type[0]);
			//object propval = propvalConstructor.Invoke(new object[0]);
			object propval = null;
			try
			{
				propval = Activator.CreateInstance(member.Type);
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception while creating a instance!");
				throw e;
			}

			//Serialize
			if (propval != null && Deserialize(buffer, member.Type, ref propval, key + "."))
			{
				member.SetValue(reference, propval);
				return true;
			}

			return false;
		}

		#region Serialize

		/// <summary>
		/// Serializes an object into a single Dictionary.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static Dictionary<string, string> Serialize(object obj)
		{
			Dictionary<string, string> buffer = new Dictionary<string, string>();
			Serialize(buffer, obj);
			return buffer;
		}

		/// <summary>
		/// Serializes a object into a single dictionary
		/// </summary>
		/// <param name="buffer">The dictionary that all the keys will be inserted into</param>
		/// <param name="obj">The object to parse</param>
		/// <param name="subkey">The current subkey</param>
		public static void Serialize(Dictionary<string, string> buffer, object obj, string subkey = "")
		{

			//Prepare the type and initial options
			Type type = obj.GetType();

			//See if we should override the options
			RedisOptionAttribute options = type.GetCustomAttribute<RedisOptionAttribute>();

			//Iterate over every property
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				SerializeMember(buffer, new SerializeMember(property), obj, subkey);

			//Iterate over every field
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				SerializeMember(buffer, new SerializeMember(field), obj, subkey);
		}		
		private static void SerializeMember(Dictionary<string, string> buffer, SerializeMember member, object reference, string subkey)
		{

			var ignoreAttribute = member.GetCustomAttribute<RedisIgnoreAttribute>();
			if (ignoreAttribute != null) return;

			//Ignore generated blocks
			var generatedAttribute = member.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
			if (generatedAttribute != null) return;


			var attribute = member.GetCustomAttribute<RedisPropertyAttribute>();
            if (attribute == null) attribute = new RedisPropertyAttribute(member.Name);

			//Prepare its key
			string key = PrepareKey(attribute, member.Name, subkey);

			//If it has a serialize attribute, we want to construct its serializer
			//var serializerAttribute = member.GetCustomAttribute<RedisSerializerAttribute>();
			//if (serializerAttribute != null)
			//{
			//	//They have a custom serializer, so lets construct its type
			//	var constructor = serializerAttribute.Serializer.GetConstructor(new Type[0]);
			//	if (constructor != null)
			//	{
			//		var serializer = constructor.Invoke(new object[0]) as RedisSerializer;
			//		if (serializer != null)
			//		{
			//			serializer.Serialize(buffer, member, reference, key);
			//			return;
			//		}
            //
			//		throw new Exception("Bad Serialization on the custom serializer! Failed to cast into a RedisSerializer");
			//	}
            //
			//	throw new Exception("Bad Serialization on the custom serializer! Failed to find a constructor with 0 elements");
			//}

			//Make sure its a object
			if (member.IsPrimitive || member.IsEnum || member.IsString)
			{
				//Add it to the dictionary
				object v = member.GetValue(reference);
				if (v != null) buffer.Add(key, v.ToString());
				return;
			}

			//Everything else fails, so do classical serialization
			object propval = member.GetValue(reference);
			if (propval != null)
                Serialize(buffer, propval, key + ".");
		}

		#endregion
		
		private static T Construct<T>()
		{
			var t = typeof(T);
			var constructor = t.GetConstructor(new Type[0]);
			return (T)constructor.Invoke(new object[0]);
		}
		
		/// <summary>
		/// Creates a hashmap key name
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="prop"></param>
		/// <param name="subkey"></param>
		/// <returns></returns>
		private static string PrepareKey(RedisPropertyAttribute attr, string propName, string subkey)
		{
			return subkey + (attr != null && attr.DisplayName != null ? attr.DisplayName : propName);
		}
	}

	/// <summary>
	/// Serialized Member
	/// </summary>
	public class SerializeMember
	{
		public PropertyInfo Property { get; set; }
		public FieldInfo Field { get; set; }
		public string Name { get; set; }
		public Type Type { get; set; }
		public bool IsProperty => Property != null;
		public bool IsField => Field != null;

		public bool IsPrimitive => Type.IsPrimitive;
		public bool IsEnum => Type.IsEnum;
		public bool IsString => Type == typeof(string);

		public SerializeMember(PropertyInfo property)
		{
			Property = property;
			Field = null;
			Name = property.Name;
			Type = property.PropertyType;
		}

		public SerializeMember(FieldInfo field)
		{
			Property = null;
			Field = field;
			Name = field.Name;
			Type = field.FieldType;
		}

		public object GetValue(object reference)
		{
			return IsProperty ? Property.GetValue(reference) : Field.GetValue(reference);
		}
		public void SetValue(object reference, object value)
		{
			if (IsProperty)
				Property.SetValue(reference, value);
			else
				Field.SetValue(reference, value);
		}

		public T GetCustomAttribute<T>() where T : Attribute
		{
			if (IsProperty)
				return Property.GetCustomAttribute<T>();
			return Field.GetCustomAttribute<T>();
		}

		public MemberInfo GetMember()
		{
			return IsProperty ? (MemberInfo)Property : (MemberInfo)Field;
		}
		public static implicit operator MemberInfo(SerializeMember member)
		{
			return member.GetMember();
		}
	}
}
