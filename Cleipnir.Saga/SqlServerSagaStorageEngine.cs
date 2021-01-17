using System.Data.SqlClient;
using Cleipnir.StorageEngine;
using Cleipnir.StorageEngine.SqlServer;
using Dapper;

namespace Cleipnir.Saga
{
    public class SqlServerSagaStorageEngine : ISagaStorageEngine
    {
        private readonly string _databaseHostName;
        private readonly string _databaseName;

        public SqlServerSagaStorageEngine(string databaseHostName, string databaseName)
        {
            _databaseHostName = databaseHostName;
            _databaseName = databaseName;
        }

        public int AtVersion(string groupId)
        {
                using var conn = CreateConnection();
                var atVersion = conn.QuerySingle<int>(
                    "SELECT AtVersion FROM GroupVersions WHERE GroupId = @GroupId",
                    new {GroupId = groupId }
                );

                return atVersion;
        }

        public IStorageEngine GetStorageEngine(string instanceId, string _)
        {
            return new SqlServerStorageEngine(instanceId, DatabaseHelper.ConnectionString(_databaseHostName, _databaseName));
        }

        private SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(DatabaseHelper.ConnectionString(_databaseHostName, _databaseName));
            connection.Open();

            return connection;
        }

        public void Initialize()
        {
            DatabaseHelper.CreateDatabaseIfNotExist(_databaseHostName, _databaseName);

            const string createTableSql = @"
              IF OBJECT_ID('dbo.GroupVersions', 'U') IS NULL 
                CREATE TABLE [dbo].[GroupVersions](
	                [GroupId] [varchar](50) PRIMARY KEY NOT NULL,
	                [AtVersion] [int] NOT NULL
                )";

            using var conn = CreateConnection();
            conn.Execute(createTableSql);
        }
    }
}
