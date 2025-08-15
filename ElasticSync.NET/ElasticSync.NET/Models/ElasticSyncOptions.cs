using System;
using System.Collections.Generic;

namespace ElasticSync.Models;

public class ElasticSyncOptions
{
    public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.PostgreSQL;
    public string ConnectionString { get; set; } = default!;
    public string ElasticsearchUrl { get; set; } = default!;
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayInSeconds { get; set; } = 5; //base for exponential backoff
    public int BatchSize { get; set; } = 500; //Number of logs to sync in one batch
    public ElasticSyncMode Mode { get; set; } = ElasticSyncMode.Realtime;
    public int IntervalInSeconds { get; set; } = 60;//Only applicable for Interval mode
    public bool EnableMultipleWorker { get; private set; } = false;
    public List<TrackedEntity> Entities { get; set; } = new();
    public WorkerOptions WorkerOptions { get; private set; } = new WorkerOptions();

    public void UsePostgreSql(string connectionString)
    {
        DatabaseProvider = DatabaseProvider.PostgreSQL;
        ConnectionString = connectionString;
    }
    public void EnableMultipleWorkers(WorkerOptions? options = null)
    {
        EnableMultipleWorker = true;
        WorkerOptions = options ?? new WorkerOptions();
    }

}

public class WorkerOptions
{
    public int BatchSizePerWorker { get; set; } = 250;
    public int NumberOfWorkers { get; set; } = 2; //number of parallel worker

    public WorkerOptions(int batchSizePerWorker = 250, int numberOfWorkers = 2)
    {
        BatchSizePerWorker = batchSizePerWorker;
        NumberOfWorkers = numberOfWorkers;
    }
}

public class TrackedEntity
{
    public string Table { get; set; } = default!;
    public string PrimaryKey { get; set; } = "id";
    public Type EntityType { get; set; } = default!;
    public string? IndexName { get; set; }
    public string? IndexVersion { get; set; }
    public Action<Nest.TypeMappingDescriptor<object>>? CustomMapping { get; set; }
}

public enum ElasticSyncMode
{
    Realtime,
    Interval
}

public enum DatabaseProvider
{
    PostgreSQL,
    SqlServer,
    MySql,
    Sqlite
}