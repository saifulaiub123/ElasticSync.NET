using ElasticSync.NET.Builder;
using System;
using System.Collections.Generic;

namespace ElasticSync.Models;

public interface IDatabaseConfigurationHandler
{
    void SetDatabaseProvider(DatabaseProvider provider, string connectionString);
}

public class ElasticSyncOptions : IDatabaseConfigurationHandler
{
    
    public string ElasticsearchUrl { get; set; } = default!;
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayInSeconds { get; set; } = 5; //base for exponential backoff
    public int BatchSize { get; private set; } = 500; //Number of logs to sync in one batch
    public SyncMode SyncMode { get; private set; }
    public int IntervalInSeconds { get; private set; } = 60; //Only applicable for Interval mode
    public bool IsMultipleWorkers { get; private set; } = false;
    public List<TrackedEntity> Entities { get; set; } = new();
    public WorkerOptions WorkerOptions { get; private set; } = new WorkerOptions();

    internal DatabaseProvider DatabaseProvider { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;


    public RealTimeSyncBuilder RealTimeSync(int batchSize = 500)
    {
        if (IsIntervalSync)
            throw new InvalidOperationException("Interval sync mode already set. Cannot set both RealTimeSync and IntervalSync.");

        IsRealTimeSync = true;
        SyncMode = SyncMode.RealTime;
        BatchSize = batchSize;

        return new RealTimeSyncBuilder(this);
    }

    public IntervalSyncBuilder IntervalSync(int intervalInSeconds = 60, int batchSize = 500)
    {
        if (IsRealTimeSync != null)
            throw new InvalidOperationException("RealTime sync mode already set. Cannot set both RealTimeSync and IntervalSync.");

        IsIntervalSync = true;
        SyncMode = SyncMode.Interval;
        IntervalInSeconds = intervalInSeconds;
        BatchSize = batchSize;

        return new IntervalSyncBuilder(this);
    }

    internal void EnableMultipleWorkers(WorkerOptions? options = null)
    {
        IsMultipleWorkers = true;
        WorkerOptions = options ?? new WorkerOptions();
    }

    void IDatabaseConfigurationHandler.SetDatabaseProvider(DatabaseProvider provider, string connectionString)
    {
        DatabaseProvider = provider;
        ConnectionString = connectionString;
    }

    public bool IsRealTimeSync { get; private set; }
    public bool IsIntervalSync { get; private set; }

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

