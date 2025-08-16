using ElasticSync.NET.Builder;
using ElasticSync.NET.Enum;
using ElasticSync.NET.Interface;
using System;
using System.Collections.Generic;

namespace ElasticSync.NET.Models;

public class ElasticSyncOptions : IDatabaseConfigurationHandler
{
    /// <summary>
    /// URL of the Elasticsearch instance to sync logs to. This is required for syncing logs.
    /// </summary>
    public string ElasticsearchUrl { get; set; } = default!;
    /// <summary>
    /// Maximum number of retries for a failed operation. This is used for retrying failed operations like indexing logs to Elasticsearch.
    /// </summary>
    public int MaxRetries { get; set; } = 5;
    /// <summary>
    /// Delay in seconds before retrying a failed operation.
    /// </summary>
    public int RetryDelayInSeconds { get; set; } = 5; //base for exponential backoff
    /// <summary>
    /// Batch size for syncing logs to Elasticsearch. This is not applicable for multiple workkers scenerio.
    /// </summary>
    public int BatchSize { get; private set; } = 500; //Number of logs to sync in one batch
    public SyncMode SyncMode { get; private set; }
    /// <summary>
    /// Interval in seconds for syncing logs to Elasticsearch. This is only applicable for Interval mode.
    /// </summary>
    public int IntervalInSeconds { get; private set; } = 60; //Only applicable for Interval mode
    /// <summary>
    /// List of entities to track for changes. Each entity should have a table name, primary key, and entity type.
    /// </summary>
    public List<TrackedEntity> Entities { get; set; } = new();
    //public Action<Nest.TypeMappingDescriptor<object>>? CustomMapping { get; set; }

    /// <summary>
    /// Enable multiple workers for syncing logs. This will create multiple parallel workers to process logs. Efficient for high volume data changes
    /// </summary>
    public bool IsMultipleWorkers { get; private set; } = false;
    /// <summary>
    /// Options for configuring workers when multiple workers are enabled.
    /// </summary>
    public WorkerOptions WorkerOptions { get; private set; } = new WorkerOptions();

    /// <summary>
    /// Database provider for the application. This is used to configure the database connection.
    /// </summary>
    internal DatabaseProvider DatabaseProvider { get; private set; }
    /// <summary>
    /// Connection string for the database. This is used to connect to the database.
    /// </summary>
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
        if (IsRealTimeSync)
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
    private string? _indexName;

    /// <summary>
    /// Case sensetive name of the table in the database. This is used to track changes in the table.
    /// </summary>
    public string Table { get; set; }
    /// <summary>
    /// Primary key of the entity. This is used to identify the record in Elasticsearch. Default is "id".
    /// </summary>
    public string PrimaryKey { get; set; } = "id";
    /// <summary>
    /// Type of the entity. This is used for deserialization of the payload.
    /// </summary>
    public Type EntityType { get; set; } = default!;
    /// <summary>
    /// Index name in Elasticsearch. If not provided, it will default to the table name with lower case.
    /// </summary>
    public string IndexName 
    { 
        get => _indexName ?? Table.ToLowerInvariant(); 
        set => _indexName = value; 
    }
    /// <summary>
    /// 
    /// </summary>
    public string? IndexVersion { get; set; }
    //public Action<Nest.TypeMappingDescriptor<object>>? CustomMapping { get; set; }
}

