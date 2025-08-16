using ElasticSync.NET.Models;
using ElasticSync.NET.Enum;
using ElasticSync.NET.Interface;
using ElasticSync.NET.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ElasticSync.Net.PostgreSql.Services
{
    public class PostgreDbConfigurations : IDatabaseServiceProvider
    {
        private readonly string _connectionString;

        public PostgreDbConfigurations(string connectionString)
        {
            if (connectionString == null)
                throw new InvalidOperationException("Database connection string is not configured.");

            _connectionString = connectionString;
        }

        public DatabaseProvider Provider => DatabaseProvider.SqlServer;

        public void Configure(ElasticSyncOptions options, IServiceCollection services)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            ((IDatabaseConfigurationHandler)options).SetDatabaseProvider(DatabaseProvider.PostgreSQL, _connectionString);

            services.AddSingleton<IChangeLogService, PostgreChangeLogService>();
            services.AddSingleton<IInstallerService, PostgreInstallerService>();
        }
    } 
}
