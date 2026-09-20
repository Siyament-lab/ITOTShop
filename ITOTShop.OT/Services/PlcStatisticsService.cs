using ITOTShop.OT.Models;

namespace ITOTShop.OT.Services
    {
    public class PlcStatisticsService
        {
        public int SucceededOrders { get; private set; }
        public int FailedOrders { get; private set; }

        public int TotalOrders =>
            SucceededOrders + FailedOrders;


        // Sparar senaste order som redan har räknats som lyckad.
        private ushort? _lastSucceededOrderId;


        // Sparar senaste order som redan har räknats som misslyckad.
        private ushort? _lastFailedOrderId;


        public void UpdateStatistics ( ProductionState productionState )
            {
            // Räknar en order som lyckad när PLC rapporterar Completed.
            // Samma order får endast räknas en gång.
            if ((productionState.ProductionStatus == 2 ||
                 productionState.ProductionComplete) &&
                productionState.OrderId != 0 &&
                _lastSucceededOrderId != productionState.OrderId)
                {
                SucceededOrders++;

                _lastSucceededOrderId =
                    productionState.OrderId;
                }


            // Räknar en order som misslyckad när PLC rapporterar Fault.
            // Samma order får endast räknas en gång.
            if ((productionState.ProductionStatus == 3 ||
                 productionState.ProductionFault) &&
                productionState.OrderId != 0 &&
                _lastFailedOrderId != productionState.OrderId)
                {
                FailedOrders++;

                _lastFailedOrderId =
                    productionState.OrderId;
                }
            }
        }
    }