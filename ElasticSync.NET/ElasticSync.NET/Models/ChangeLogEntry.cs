using System;

namespace ChangeSync.Elastic.Postgres.Models;

public class ChangeLogEntry
{
    public int Id { get; set; }
    public string TableName { get; set; } = default!;
    public string Operation { get; set; } = default!;
    public string RecordId { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public bool Processed { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public bool DeadLetter { get; set; }
    public DateTime CreatedAt { get; set; }
}