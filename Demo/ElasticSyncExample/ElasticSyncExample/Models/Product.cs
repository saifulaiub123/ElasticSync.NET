namespace ElasticSyncExample.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        //public Category Category { get; set; } = null!;

        public int SupplierId { get; set; }
        //public supplier Supplier { get; set; } = null!;
    }
}
