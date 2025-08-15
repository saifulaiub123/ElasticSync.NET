using ElasticSync.Extensions;
using ElasticSync.Models;
using ElasticSync.Net.PostgreSql.Services;
using ElasticSyncExample;
using ElasticSyncExample.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DbConnectionString")));

builder.Services.AddElasticSyncEngine<PostgreeChangeLogService>(options =>
{
    options.UsePostgreSql(builder.Configuration.GetConnectionString("DbConnectionString"));
    options.ElasticsearchUrl = builder.Configuration["Elasticsearch:Uri"];
    options.Mode = ElasticSyncMode.Interval;
    options.IntervalInSeconds = 20;
    options.BatchSize = 500;
    options.MaxRetries = 5;
    options.RetryDelayInSeconds = 20;
    options.EnableMultipleWorker = false;
    //options.WorkerOptions.NumberOfWorkers = 4;
    options.Entities = new List<TrackedEntity>
    {
        new TrackedEntity { Table = "Customers", EntityType = typeof(Customer), PrimaryKey = "Id", IndexName = "customers" },
        //new TrackedEntity { Table = "Orders", EntityType = typeof(Order), PrimaryKey = "Id", IndexName = "orders" },
        //new TrackedEntity { Table = "OrderItems", EntityType = typeof(OrderItem), PrimaryKey = "Id", IndexName = "orderitems" },
        //new TrackedEntity { Table = "Products", EntityType = typeof(Product), PrimaryKey = "Id", IndexName = "products" },
        //new TrackedEntity { Table = "Employees", EntityType = typeof(Employee), PrimaryKey = "Id", IndexName = "employees" },
        //new TrackedEntity { Table = "Departments", EntityType = typeof(Department), PrimaryKey = "Id", IndexName = "departments" },
        //new TrackedEntity { Table = "Addresses", EntityType = typeof(Address), PrimaryKey = "Id", IndexName = "addresses" },
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    //(new Seed()).SeedAsync(db).GetAwaiter().GetResult();
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
