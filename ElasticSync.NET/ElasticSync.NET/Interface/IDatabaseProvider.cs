
using ElasticSync.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ElasticSync.NET.Interface
{
    public interface IDatabaseServiceProvider
    {
        /// <summary>
        /// Configure ElasticSyncOptions and register DB-specific services
        /// </summary>
        DatabaseProvider Provider { get; }

        void Configure(ElasticSyncOptions option, IServiceCollection services);

    }
}
