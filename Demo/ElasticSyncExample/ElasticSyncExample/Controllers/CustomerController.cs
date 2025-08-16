using ElasticSyncExample.Models;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace ElasticSyncExample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public CustomerController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost(Name = "AddCustomer")]
        public IActionResult Add()
        {
            var random = new Random();
            var rnd = (random.NextDouble() * 100 + 1);

            var customer = new Customer()
            {
                Name = $"Customer {rnd}",
                Email = $"customer{rnd}@example.com",
            };

            _dbContext.Customers.Add(customer);

            // Save customers first so they get Ids (if needed for FK)
            _dbContext.SaveChanges();
            return Ok();
        }
    }
}
