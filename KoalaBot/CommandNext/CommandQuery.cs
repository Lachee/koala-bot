using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.CommandNext
{
    /// <summary>
    /// A Query that handles conversion of types from Commands.
    /// </summary>
    public class CommandQuery : Query
    {
        /// <summary>
        /// The command next extension
        /// </summary>
        public CommandsNextExtension CommandsNext => Context.CommandsNext;
        
        /// <summary>
        /// The command context
        /// </summary>
        public CommandContext Context { get; }

        /// <summary>
        /// Creates a new Query Command with a context.
        /// </summary>
        /// <param name="ctx"></param>
        public CommandQuery(CommandContext ctx) : base()
        {
            Context = ctx;
        }

        /// <summary>
        /// Converts a argument to the specific type. Will not throw "KeyNotFound".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<Optional<T>> GetOptionalArgumentAsync<T>(string key)
        {
            try
            {
                T value = await ConvertArgumentAsync<T>(key);
                return Optional.FromValue<T>(value);
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentNullException e) { throw new ArgumentNullException(key, e.Message); }
            catch (ArgumentException e) { throw new ArgumentException(e.Message, key, e); }

            return Optional.FromNoValue<T>();
        }

        /// <summary>
        /// Converts a argument to the specific type. Will not throw "KeyNotFound".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<T> GetArgumentAsync<T>(string param, T @default = default(T))
        {
            try
            {
                T value = await ConvertArgumentAsync<T>(param);
                return value;
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentNullException e) { throw new ArgumentNullException(param, e.Message); }
            catch (ArgumentException e) { throw new ArgumentException(e.Message, param); }

            return @default;
        }

        /// <summary>
        /// Converts a argument to the specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> ConvertArgumentAsync<T>(string key)
        {
            if (TryGetString(key, out var str))
                return (T) (await CommandsNext.ConvertArgument<T>(str, Context));

            throw new KeyNotFoundException($"There is no value for the specified argument {key}");
        }
    }
}
