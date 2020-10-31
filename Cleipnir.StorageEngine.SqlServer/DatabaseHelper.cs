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

        public static SqlConnection CreateConnection(string server, string databaseName)
            => CreateConnection(ConnectionString(server, databaseName));

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
    }
}
