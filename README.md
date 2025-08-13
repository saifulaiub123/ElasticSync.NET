# ElasticSync.NET

**ElasticSync.NET** is a high-performance .NET library that synchronizes PostgreSQL data to Elasticsearch in **real-time** with built-in reliability, scalability, and monitoring.

It’s designed for teams that want **instant search indexing** without building and maintaining complex change-tracking pipelines.

---

## ✨ Features

- **Real-time sync** from Relational Database to Elasticsearch
- **Supported Database** PostgreSQL
- **Multiple entity type support** (e.g., Products, Orders, Customers)
- **Automatic trigger & change tracking** in PostgreSQL
- **Parallel workers** Pending...
- **Retry logic** for transient errors
- **Bulk indexing** for performance optimization
- **Optional dashboard** Pending...

---

## 🚀 Getting Started

### Install the NuGet Package

```sh
dotnet add package ElasticSync.NET
```

# Requirements
.NET 6, .NET 7, or .NET 8
PostgreSQL 13+ (with trigger support)
Elasticsearch 8.x (or compatible OpenSearch version)

# Basic Usage
```sh
builder.Services.AddElasticSyncEngine(options =>
{
    options.PostgresConnectionString = builder.Configuration.GetConnectionString("DbConnectionString");
    options.ElasticsearchUrl = builder.Configuration["ElasticsearchUri"];
    options.Mode = ElasticSyncMode.Interval;//Default ElasticSyncMode.RealTime
    options.PollIntervalSeconds = 5;
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id", IndexName = "customers" },
        new TrackedEntity { Table = "Products", EntityType = typeof(Product), PrimaryKey = "Id", IndexName = "products" },
    };
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
