using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.OT.Models
    {
    // Läser in från PLC:n
    public class ProductionState
        {
        public ushort OrderId { get; set; }
        public ushort RequestedQuantity { get; set; }
        public ushort ProductionStatus { get; set; }
        public ushort ProducedQuantity { get; set; }
        public ushort FaultCode { get; set; }
        public bool ProductionComplete { get; set; }
        public bool ProductionFault { get; set; }


        }
    }
