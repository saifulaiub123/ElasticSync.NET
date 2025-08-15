using System;
using System.Collections.Generic;

namespace ElasticSync.Models;

public class ElasticSyncOptions
{
    public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.PostgreSQL;
    public string ConnectionString { get; private set; } = default!;
    public string ElasticsearchUrl { get; set; } = default!;
    public int MaxRetries { get; set; } = 5;
    public int RetryDelayInSeconds { get; set; } = 5; //base for exponential backoff
    public int BatchSize { get; private set; } = 500; //Number of logs to sync in one batch
    public SyncOptions? SyncOptions { get; private set; }
    public SyncMode Mode { get; private set; } = SyncMode.RealTime;
    public int IntervalInSeconds { get; private set; } = 60; //Only applicable for Interval mode
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



    public void RealTimeSync(int batchSize = 500)
    {
        if (SyncOptions != null)
            throw new InvalidOperationException("Interval sync mode already set. Cannot set both RealTimeSync and IntervalSync.");
        SyncOptions = SyncOptions.RealTime(batchSize);
    }

    public void IntervalSync(int intervalInSeconds = 60, int batchSize = 500)
    {
        if (SyncOptions != null)
            throw new InvalidOperationException("RealTime sync mode already set. Cannot set both RealTimeSync and IntervalSync.");
        SyncOptions = SyncOptions.Interval(intervalInSeconds, batchSize);
    }
    public bool IsRealTime => SyncOptions?.Mode == SyncMode.RealTime;
    public bool IsInterval => SyncOptions?.Mode == SyncMode.Interval;

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

public class SyncOptions
{
    public SyncMode Mode { get; private set; }
    public int BatchSize { get; private set; }
    public int? IntervalInSeconds { get; private set; }

    private SyncOptions(SyncMode mode, int batchSize, int? intervalInSeconds = null)
    {
        Mode = mode;
        BatchSize = batchSize;
        IntervalInSeconds = intervalInSeconds;
    }

    public static SyncOptions RealTime(int batchSize) =>
        new SyncOptions(SyncMode.RealTime, batchSize);

    public static SyncOptions Interval(int intervalInSeconds, int batchSize) =>
        new SyncOptions(SyncMode.Interval, batchSize, intervalInSeconds);
}

public enum SyncMode
{
    RealTime,
    Interval
}

public enum DatabaseProvider
{
    PostgreSQL,
    SqlServer,
    MySql,
    Sqlite
}