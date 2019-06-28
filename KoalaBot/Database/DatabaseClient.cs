using KoalaBot.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Database
{
    public class DatabaseClient : IDisposable
    {
        public string Server { get; }
        public string Database { get; }
        public string Prefix { get; set; }
        public bool IsConnected { get; private set; }
        public Logger Logger { get; }

        public DateTime LastStatementPreparedAt { get; private set; }

        private string constr;
        private MySqlConnection _connection;

        public DatabaseClient(string server, string database, string username, string password, string prefix = "k_", int port = 3306, Logger logger = null)
        {
            this.Server = server;
            this.Database = database;
            this.Prefix = prefix;
            this.LastStatementPreparedAt = DateTime.Now;
            this.constr = $"SERVER={server};DATABASE={database};USERNAME={username};PASSWORD={password};";
            //this.constr = $"Server={server};Database={database};Uid={username};Psw={password};";

            _connection = new MySqlConnection(constr);
            Logger = logger ?? new Logger("SQL");
        }

        /// <summary>
        /// Selects just one elmement from the query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<bool> ExecuteOneAsync(string query, Func<DbDataReader, Task> action, Dictionary<string, object> arguments = null)
        {
            var cmd = await CreateCommand(query, arguments);
            if (cmd == null) return false;

            var reader = await cmd.ExecuteReaderAsync();
            if (reader == null) return false;

            if (!await reader.ReadAsync()) return false;
            await action.Invoke(reader);
            return true;
        }

        /// <summary>
        /// Selects all the elements in the query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="callback"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<List<T>> ExecuteAsync<T>(string query, Func<DbDataReader, Task<T>> callback, Dictionary<string, object> arguments = null)
        {
            var cmd = await CreateCommand(query, arguments);
            if (cmd == null) return null;

            var reader = await cmd.ExecuteReaderAsync();
            if (reader == null) return null;

            List<T> elements = new List<T>();
            while (await reader.ReadAsync())
                elements.Add(await callback.Invoke(reader));

            return elements;
        }


        /// <summary>
        /// Inserts or Updates the data asyncronously
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<long> InsertOrUpdateAsync(string table, Dictionary<string, object> columns = null)
        {
            StringBuilder qb = new StringBuilder();
            qb.Append("INSERT INTO ").Append(table).Append(" (").AppendJoin(",", columns.Keys).Append(") ");
            qb.Append("VALUES (").AppendJoin(",", columns.Keys.Select(v => "?" + v)).Append(") ");
            qb.Append("ON DUPLICATE KEY UPDATE ").AppendJoin(",", columns.Keys.Select(v => v + "=?" + v));

            string query = qb.ToString();
            var cmd = await CreateCommand(query, columns);
            if (cmd == null) return 0;

            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        }


        /// <summary>
        /// Creates a command with arguments
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<MySqlCommand> CreateCommand(string query)
        {
            //Validate the query
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException("query");

            //Try connect
            if (!await OpenAsync()) return null;

            //Update the timer and prepare the query
            LastStatementPreparedAt = DateTime.Now;
            query = query.Replace("!", Prefix);
            var cmd = new MySqlCommand(query, _connection);
            return cmd;
        }

        /// <summary>
        /// Creates a command with named arguments
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<MySqlCommand> CreateCommand(string query, Dictionary<string, object> arguments = null)
        {
            var cmd = await CreateCommand(query);
            if (cmd == null) return null;

            if (arguments != null)
                foreach (var kp in arguments)
                    cmd.Parameters.AddWithValue(kp.Key, kp.Value);

            return cmd;
        }

        /// <summary>
        /// Opens the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> OpenAsync()
        {
            try
            {
                if (IsConnected)
                    return true;
                
                Logger.Log("Attempting Connection...");
                await _connection.OpenAsync();
                IsConnected = true;
                return true;
            }
            catch(Exception e)
            {
                Logger.LogError(e, "SQL Open Exception.");
                await CloseAsync();
                return false;
            }
        }
        /// <summary>
        /// Closes the database
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                Logger.Log("Closing SQL");
                await _connection.CloseAsync();
                IsConnected = false;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "SQL Close Exception.");
                DisposeConnection();
            }
        }

        /// <summary>
        /// Disposes the connection
        /// </summary>
        public void DisposeConnection()
        {
            Logger.Log("Disposing Connection...");
            if (_connection != null)
            {
                if (IsConnected)
                {
                    _connection.Close();
                    IsConnected = false;
                }

                _connection.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// Disposes the connection
        /// </summary>
        public void Dispose() { DisposeConnection(); }
    }
}
