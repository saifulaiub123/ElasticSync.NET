namespace ElasticSyncExample.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public Address? Address { get; set; }
    }
}
