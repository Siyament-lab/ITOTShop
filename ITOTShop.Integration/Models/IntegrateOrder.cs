using ITOTShop.Integration.Enums;

namespace ITOTShop.Integration.Models
    {
    public class IntegrateOrder
        {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public OrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        }
    }