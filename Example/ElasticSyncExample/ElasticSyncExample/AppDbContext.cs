using ElasticSyncExample.Models;
using Microsoft.EntityFrameworkCore;

namespace ElasticSyncExample
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Product> Products => Set<Product>();
        //public DbSet<Category> Categories => Set<Category>();
        //public DbSet<supplier> Suppliers => Set<supplier>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Address> Addresses => Set<Address>();
        //public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<OrderItem>()
            //    .HasKey(oi => new { oi.OrderId, oi.ProductId });
        }
    }
}