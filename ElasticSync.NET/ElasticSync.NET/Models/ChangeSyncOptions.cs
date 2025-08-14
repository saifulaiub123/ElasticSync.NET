using System;
using System.Collections.Generic;

namespace ChangeSync.Elastic.Postgres.Models;

public class ChangeSyncOptions
{
    public string PostgresConnectionString { get; set; } = default!;
    public string ElasticsearchUrl { get; set; } = default!;
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayInSeconds { get; set; } = 5; //base for exponential backoff
    public int BatchSize { get; set; } = 500; //Number of logs to sync in one batch
    public ElasticSyncMode Mode { get; set; } = ElasticSyncMode.Realtime;
    public int PollIntervalSeconds { get; set; } = 60;//Only applicable for Interval mode
    public bool EnableParallelProcessing { get; set; } = false;
    public List<TrackedEntity> Entities { get; set; } = new();
    public WorkerOptions WorkerOptions { get; set; } = new WorkerOptions();

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

public class WorkerOptions
{
    public int BatchSizePerWorker { get; set; } = 250;
    public int NumberOfWorkers { get; set; } = 4; //number of parallel worker
    //public int MaxRetries { get; set; } = 5;
    public int NotifyListenerPollMs { get; set; } = 1000; //fallback poll interval
    public string WorkerId { get; set; } = Guid.NewGuid().ToString("N");
}

public enum ElasticSyncMode
{
    Realtime,
    Interval
}