namespace ElasticSyncExample.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
