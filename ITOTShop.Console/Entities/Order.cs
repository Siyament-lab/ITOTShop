using System;
using System.Collections.Generic;
using System.Text;
using ITOTShop.Console.Enums;

namespace ITOTShop.Console.Entities
    {
    public class Order
        {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        //Nyskapad order är inte uppdaterad än,därför väljer vi att använda en nullable DateTime för UpdatedAt
        public DateTime? UpdatedAt { get; set; }

        }
    }
