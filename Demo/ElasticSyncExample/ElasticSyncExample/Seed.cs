using ElasticSyncExample.Models;

namespace ElasticSyncExample
{
    public class Seed
    {
        public async Task SeedAsync(AppDbContext context) 
        {
            await Task.Delay(TimeSpan.FromSeconds(20));

            var random = new Random();

            // Seed 500 customers
            var customers = new List<Customer>();
            for (int i = 1; i <= 500; i++)
            {
                customers.Add(new Customer
                {
                    Name = $"Customer {i}",
                    Email = $"customer{i}@example.com",
                    // Initialize other properties if needed
                });
            }
            context.Customers.AddRange(customers);

            // Save customers first so they get Ids (if needed for FK)
            context.SaveChanges();

            // Seed 500 products
            var products = new List<Product>();
            for (int i = 1; i <= 500; i++)
            {
                products.Add(new Product
                {
                    Name = $"Product {i}",
                    Price = (decimal)(random.NextDouble() * 100 + 1), // Price between 1 and 101
                    CategoryId = 1, // set a valid category id or random from your categories
                    SupplierId = 1  // set a valid supplier id or random from your suppliers
                                    // Initialize other properties if needed
                });
            }
            context.Products.AddRange(products);

            context.SaveChanges();
            
        }
    }
}
