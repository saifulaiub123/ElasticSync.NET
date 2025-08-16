# ElasticSync.NET

**ElasticSync.NET** is a high-performance, real-time data synchronization engine for syncing relational database changes to Elasticsearch. It supports **PostgreSQL** today, with a **Modular Core** that allows future extensions for **SQL Server**, **MySQL**, and more.

It’s designed for the teams who want a lighweight system for **Instant Search Indexing** without building and maintaining complex change-tracking pipelines or without installing any additional tools/server like Debzium, Kafka etc.

---

## ✨ Features

- **Supported database so far** PostgreSQL
- **Real-time sync** from Relational Database to Elasticsearch
- **Interval-based** syncing with configurable intervals
- **Parallel work** Configurable Parallel workers for high-throughput scenarios like (1000/2000+ rows/sec).
- **Impact on DB** Minimal impact on source database
- **Multiple entity type support** (e.g., Products, Orders, Customers)
- **Automatic trigger & change tracking** in PostgreSQL
- **Retry logic** Dead-letter queue and retry logic
- **Bulk indexing** for performance optimization
- **Dashboard and monitoring** Pending...

---

## 🚀 Sync with PostgreSql Database:
---
## ⚙️ Install the NuGet Package

```sh
dotnet add package ElasticSync.NET.PostgreSql
```

## ⚙️Requirements
.NET 6, .NET 7, or .NET 8
PostgreSQL 13+ (with trigger support)
Elasticsearch 8.x (or compatible OpenSearch version)

## ⚙️ Use Real-Time Sync

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

## ⚙️ Use Real-Time Sync With Multiple Workers for High Volume Data Changes

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

## ⚙️ Use Interval Sync

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

## ⚙️ Use Interval Sync With Multiple Workers for High Volume Data Changes

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
## ⚙️ Real Time Syncing Background Process with Diagram

<img width="1536" height="1024" alt="arch" src="https://github.com/user-attachments/assets/5b8e1cac-86ea-44b7-9fa8-1c8c18354c67" />

## 📁 Project Structure

- **ElasticSync.Net** – core interfaces, abstraction, and engine
- **ElasticSync.Net.PostgreSql** – PostgreSQL-specific implementation
- **ElasticSync.Net.SqlServer** – Coming soon

## 🧱 Uninstall/Clean Up DB Object

If you want to remove the package to clean up all the database object you need to run a script which you will find under **UninstallScript** folder. **UninstallScript** is applicable from version **1.0.1**

## 🌐 Future Plans
- Extend the project for **Sql Server** and **Mysql database**
- Support for **EntityFramework**
- Expose API to get the Statistics like **Syncing Latency**, **Pending Data to Process**, **Data Processed Count by Date**
- **Dashboard and Real Time Monitoring**

## 🙏 Contributing

We welcome contributions!
Fork the repository
Create a new branch (git checkout -b feature/my-feature)
Commit your changes (git commit -m 'Add my feature')
Push to your fork (git push origin feature/my-feature)
Open a Pull Request

## 📜 License
This project is licensed under the MIT License
MIT © 2025 **ElasticSync.NET**

## 📅 Maintainer
**ElasticSync.NET** is maintained by **Md. Saiful Islam**

For support, bug reports, or feature requests, please open an issue on GitHub.
