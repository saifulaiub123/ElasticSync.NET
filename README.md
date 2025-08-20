# ElasticSync.Net

[![NuGet](https://img.shields.io/nuget/v/ElasticSync.Net.svg)](https://www.nuget.org/packages/ElasticSync.Net)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ElasticSync.Net.svg)](https://www.nuget.org/packages/ElasticSync.Net)

**ElasticSync.Net** is a **high-performance**, **real-time** data synchronization engine for syncing relational database changes to Elasticsearch. It supports **PostgreSQL** today, with a **Modular Core** that allows future extensions for **SQL Server**, **MySQL**, and more.
It supports both **trigger-based** and **interval-based** change tracking, with **parallel processing**, **dead-letter queuing**, and **bulk indexing** support.

It’s designed for the teams who want a lighweight system for **Instant Search Indexing** without building and maintaining complex change-tracking pipelines or without installing any additional tools/server like Debzium, Kafka etc.

---

## Features

- **Supported database so far** PostgreSQL
- **Real-time sync** from Relational Database to Elasticsearch
- **Interval-based** syncing with configurable intervals
- **Parallel work** Configurable Parallel workers for high-throughput scenarios like (1000/2000+ rows/sec).
- **Impact on DB** Minimal impact on source database
- **Multiple entity type support** (e.g., Products, Orders, Customers)
- **Automatic trigger & change tracking** in PostgreSQL
- **Retry logic** Retry logic with exponential backoff
- **Dead-letter** queue for failed operations
- **Bulk indexing** for performance optimization
- **Dashboard and monitoring** Pending...
- **Modular design** (core + PostgreSQL-specific provider)
-** Metrics**-friendly architecture

---

## Performance

Tested with 1000 inserts/sec:
- Throughput: ~12,000 records/min per node (scalable with workers)
- Latency: <2 seconds (average) with trigger-based realtime mode
- Durable retry with exponential backoff

Partitioning **esnet.elastic_sync_change_log** table can improves batch processing performance significantly.


## Sync with PostgreSql Database:
---
## Install the NuGet Package

```sh
dotnet add package ElasticSync.NET.PostgreSql
```

## Requirements
.NET 8, .NET 9
PostgreSQL 13+ (with trigger support)
Elasticsearch 8.x (or compatible OpenSearch version)

## Use Real-Time Sync

```sh

builder.Services.AddElasticSyncEngine(options =>
{
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.RealTimeSync(batchSize: 500);
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

## Use Real-Time Sync With Multiple Workers for High Volume Data Changes

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

## Use Interval Sync

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

## Use Interval Sync With Multiple Workers for High Volume Data Changes

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
## Real Time Syncing Background Process with Diagram

<img width="1536" height="1024" alt="arch" src="https://github.com/user-attachments/assets/5b8e1cac-86ea-44b7-9fa8-1c8c18354c67" />

## Project Structure

- **ElasticSync.Net** – core interfaces, abstraction, and engine
- **ElasticSync.Net.PostgreSql** – PostgreSQL-specific implementation
- **ElasticSync.Net.SqlServer** – Coming soon

## Uninstall/Clean Up DB Object

If you want to remove the package to clean up all the database object you need to run a script which you will find under **UninstallScript** folder. **UninstallScript** is applicable from version **1.0.1**

## Future Plans
- Extend the project for **Sql Server** and **Mysql database**
- Expose API to get the Statistics like **Average processing lag**, **Unprocessed logs count per table**, **Success/failure rate per sync** etc
- Auth middleware for Elastic endpoints
- Dashboard and Real Time Monitoring
- Built-in metrics export

## Contributing

We welcome contributions!
Fork the repository
Create a new branch (git checkout -b feature/my-feature)
Commit your changes (git commit -m 'Add my feature')
Push to your fork (git push origin feature/my-feature)
Open a Pull Request

## License
This project is licensed under the MIT License
MIT © 2025 **ElasticSync.Net**

## 📅 Maintainer
**ElasticSync.NET** is maintained by **<a href="https://github.com/saifulaiub123">Md. Saiful Islam</a>**

For support, bug reports, or feature requests, please open an issue on GitHub.
