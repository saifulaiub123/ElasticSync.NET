using ElasticSync.Extensions;
using ElasticSync.Models;
using ElasticSync.Net.PostgreSql.Extentions;
using ElasticSync.Net.PostgreSql.Services;
using ElasticSyncExample;
using ElasticSyncExample.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DbConnectionString");

if (connectionString == null)
    throw new InvalidOperationException("Database connection string is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddElasticSyncEngine(options =>
{
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.RealTimeSync()
           .EnableMultipleWorkers(new WorkerOptions
           {
                BatchSizePerWorker = 300,
                NumberOfWorkers = 4
           });
    //options.IntervalSync(intervalInSeconds: 20, batchSize: 500)
    //       .EnableMultipleWorkers(new WorkerOptions
    //       {
    //           BatchSizePerWorker = 300,
    //           NumberOfWorkers = 4 //number of parallel worker
    //       });
    options.MaxRetries = 5;
    options.RetryDelayInSeconds = 20; 
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id" }
    }; 
}, (options, services) =>
{
    options.AddElasticSyncPostgreSqlServices(services, connectionString);
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
