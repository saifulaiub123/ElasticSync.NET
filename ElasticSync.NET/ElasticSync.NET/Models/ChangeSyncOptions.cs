using System;
using System.Collections.Generic;

namespace ChangeSync.Elastic.Postgres.Models;

public class ChangeSyncOptions
{
    public string PostgresConnectionString { get; set; } = default!;
    public string ElasticsearchUrl { get; set; } = default!;
    public int MaxRetries { get; set; } = 5;
    public ElasticSyncMode Mode { get; set; } = ElasticSyncMode.Realtime;
    public int PollIntervalSeconds { get; set; } = 60;
    public List<TrackedEntity> Entities { get; set; } = new();
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