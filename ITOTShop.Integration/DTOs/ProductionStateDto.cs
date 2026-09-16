using ITOTShop.Integration.Enums;

namespace ITOTShop.Integration.DTOs
    {
    // Objekt som ska samla information vi läser från PLCn
    public class ProductionStateDto
        {
        public ProductionStatus Status { get; set; }

        public int ProducedQuantity { get; set; }

        public int FaultCode { get; set; }

        public bool ProductionComplete { get; set; }

        public bool ProductionFault { get; set; }
        }
    }