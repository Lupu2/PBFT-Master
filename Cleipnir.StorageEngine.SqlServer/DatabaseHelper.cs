using System.Data.SqlClient;
using Dapper;

namespace Cleipnir.StorageEngine.SqlServer
{
    public static class DatabaseHelper
    {
        public static void CreateDatabaseIfNotExist(string server, string databaseName)
        {
            using var conn = CreateConnection(ConnectionString(server));

            conn.Execute($"IF (db_id(N'{databaseName}') IS NULL) CREATE DATABASE {databaseName}");
        }
        
        public static void CreateDatabaseIfNotExist(string server, string databaseName, string user, string password)
        {
            using var conn = CreateConnection(ConnectionString(server, user, password));

            conn.Execute($"IF (db_id(N'{databaseName}') IS NULL) CREATE DATABASE {databaseName}");
        }


        public static SqlConnection CreateConnection(string server, string databaseName)
            => CreateConnection(ConnectionString(server, databaseName));

        public static SqlConnection CreateConnection(string server, string databaseName, string user, string password)
            => CreateConnection(ConnectionString(server, databaseName, user, password));
        
        private static SqlConnection CreateConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();

            return connection;
        }

        public static string ConnectionString(string server, string databaseName)
            => $"Server={server}; Database={databaseName}; Integrated Security=SSPI;";

        public static string ConnectionString(string server)
            => $"Server={server}; Integrated Security=SSPI;";

        public static string ConnectionString(string server, string user, string password)
            =>  $"Server={server}; User={user}; Password={password}";

        
        public static string ConnectionString(string server, string databaseName, string user, string password)
            => $"Server={server}; Database={databaseName}; User={user}; Password={password};";
    }
}
