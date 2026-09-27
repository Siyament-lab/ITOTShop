namespace ITOTShop.Integration.DTOs
    {
    public class OrderStatisticsDto
        {
        public int CompletedOrders { get; set; }

        public int FailedOrders { get; set; }

        public long ProducedUnits { get; set; }
        }
    }