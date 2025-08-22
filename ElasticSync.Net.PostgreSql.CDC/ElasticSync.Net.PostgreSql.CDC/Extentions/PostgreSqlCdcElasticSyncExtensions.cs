using ElasticSync.Net.PostgreSql.CDC.Services;
using ElasticSync.NET.Enum;
using ElasticSync.NET.Interface;
using ElasticSync.NET.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticSync.Net.PostgreSql.CDC.Extentions
{
    public static class PostgreSqlCdcElasticSyncExtensions
    {
        public static void AddElasticSyncPostgreSqlCdcServices(
            this ElasticSyncOptions options,
            IServiceCollection services,
            string connectionString)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Configure internal options
            ((IDatabaseConfigurationHandler)options)
                .SetDatabaseProvider(DatabaseProvider.PostgreSQL, connectionString);

            services.AddSingleton<IChangeLogService, PostgreCdcChangeLogService>();

            services.AddHostedService<SyncListenerService>();
        }
    }
}
