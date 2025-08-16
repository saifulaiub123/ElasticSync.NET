# ElasticSync.NET

**ElasticSync.NET** is a high-performance .NET library that synchronizes PostgreSQL data to Elasticsearch in **real-time** with built-in reliability, scalability, and monitoring.

It’s designed for the teams that want a lighweight system for **instant search indexing** without building and maintaining complex change-tracking pipelines or without installing any additional tools/server like Debzium, Kafka etc

---

## ✨ Features

- **Real-time sync** from Relational Database to Elasticsearch
- **Supported Database** PostgreSQL
- **Interval-based** syncing with configurable intervals
- **Parallel work** Configurable parallel processing supported for high volume data changes.
- **Multiple entity type support** (e.g., Products, Orders, Customers)
- **Automatic trigger & change tracking** in PostgreSQL
- **Retry logic** for transient errors
- **Bulk indexing** for performance optimization
- **Optional dashboard and monitoring** Pending...

---

## 🚀 Getting Started

## To Support PostgreSql Database:

### Install the NuGet Package

```sh
dotnet add package ElasticSync.NET.PostgreSql
```

# Requirements
.NET 6, .NET 7, or .NET 8
PostgreSQL 13+ (with trigger support)
Elasticsearch 8.x (or compatible OpenSearch version)

# Basic Usage

# 1. Basic Usages

# 1.1 Use Real-Time Sync

```sh

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

# 1.2 Use Real-Time Sync With Multiple Workers for High Volume Data Changes

```sh

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

# 1.2 Use Interval Sync

```sh

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

# 1.2 Use Interval Sync With Multiple Workers for High Volume Data Changes

```sh

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
# Uninstall/Clean Up DB Object

If you want to remove the package to clean up all the database object you need to run a script which you will find under **UninstallScript** folder. **UninstallScript** is applicable from version **1.0.1**

# Contributing

We welcome contributions!
Fork the repository
Create a new branch (git checkout -b feature/my-feature)
Commit your changes (git commit -m 'Add my feature')
Push to your fork (git push origin feature/my-feature)
Open a Pull Request

# License
This project is licensed under the MIT License
