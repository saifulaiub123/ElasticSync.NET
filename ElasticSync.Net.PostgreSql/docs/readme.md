ElasticSync.NET.PostgreSql is a high-performance .NET library that synchronizes PostgreSQL data to Elasticsearch in **real-time** with built-in reliability, scalability, and monitoring.

It’s designed for the teams that want **instant search indexing** without building and maintaining complex change-tracking pipelines or withouth the need of any additional server/tools like Debzium, Kafka etc



## ✨ Features

- **Real-time sync** from PostgreSQL to Elasticsearch
- **Interval-based** syncing with configurable intervals
- **Parallel Work** Configurable parallel processing supported for high volume data changes. 
- **Multiple entity type support** (e.g., Products, Orders, Customers)
- **Automatic trigger & change tracking** in PostgreSQL
- **Retry logic** for transient errors
- **Bulk indexing** for performance optimization
- **Highly Configurable** Easy to use and configure without any additional data syncing tools

## 🚀 Getting Started

### 1. Install the NuGet Package


dotnet add package ElasticSync.NET.PostgreSql

### 1. Use it in your application

### 1.1 Use Real-Time Sync

```csharp

builder.Services.AddElasticSyncEngine(options =>
{
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.RealTimeSync();
    options.MaxRetries = 5;
    options.RetryDelayInSeconds = 20; 
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id", IndexName = "customers"},
        new TrackedEntity { Table = "Products", EntityType = typeof(Product), PrimaryKey = "Id", IndexName = "products"},
    }; 
}, (options, services) =>
{
    options.AddElasticSyncPostgreSqlServices(services, connectionString);
});

```

### 1.2 Use Real-Time Sync With Multiple Workers for High Volume Data Changes

```csharp

builder.Services.AddElasticSyncEngine(options =>
{
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.RealTimeSync()
           .EnableMultipleWorkers(new WorkerOptions
           {
                BatchSizePerWorker = 300,
                NumberOfWorkers = 4
           });
    options.MaxRetries = 5;
    options.RetryDelayInSeconds = 20; 
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id", IndexName = "customers"},
        new TrackedEntity { Table = "Products", EntityType = typeof(Product), PrimaryKey = "Id", IndexName = "products"},
    }; 
}, (options, services) =>
{
    options.AddElasticSyncPostgreSqlServices(services, connectionString);
});

```

### 1.2 Use Interval Sync

```csharp

builder.Services.AddElasticSyncEngine(options =>
{
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.IntervalSync(intervalInSeconds: 20, batchSize: 500);
    options.MaxRetries = 5;
    options.RetryDelayInSeconds = 20; 
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id", IndexName = "customers"},
        new TrackedEntity { Table = "Products", EntityType = typeof(Product), PrimaryKey = "Id", IndexName = "products"},
    }; 
}, (options, services) =>
{
    options.AddElasticSyncPostgreSqlServices(services, connectionString);
});

```

### 1.2 Use Interval Sync With Multiple Workers for High Volume Data Changes

```csharp

builder.Services.AddElasticSyncEngine(options =>
{
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.IntervalSync(intervalInSeconds: 20, batchSize: 500)
           .EnableMultipleWorkers(new WorkerOptions
           {
               BatchSizePerWorker = 300,
               NumberOfWorkers = 4 //number of parallel worker
           });
    options.MaxRetries = 5;
    options.RetryDelayInSeconds = 20; 
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id", IndexName = "customers"},
        new TrackedEntity { Table = "Products", EntityType = typeof(Product), PrimaryKey = "Id", IndexName = "products"},
    }; 
}, (options, services) =>
{
    options.AddElasticSyncPostgreSqlServices(services, connectionString);
});

```