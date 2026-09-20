using System.Diagnostics.Metrics;

namespace ITOTShop.Integration.Monitoring
    {
    public class IntegrationMetrics
        {
        // Namnet används när OpenTelemetry registrerar vår Meter.
        public const string MeterName = "ITOTShop.Integration";

        private readonly Meter _meter;

        // Kommunikationsfel är fortfarande en runtime-counter.
        private readonly Counter<long> _plcCommunicationFailures;


        // Permanenta totalsiffror hämtade från SQL.
        private int _completedOrders;
        private int _failedOrders;
        private long _producedUnits;


        // Live-värden från PLC.
        private int _plcConnected;
        private int _productionStatus;
        private int _producedQuantity;


        public IntegrationMetrics ()
            {
            _meter = new Meter (MeterName);


            // Räknar kommunikationsfel under den aktuella körningen.
            _plcCommunicationFailures =
                _meter.CreateCounter<long> (
                    "itotshop.plc.communication_failures");


            // Visar totalt antal Completed-ordrar från SQL.
            _meter.CreateObservableGauge (
                "itotshop.orders.completed",
                () => _completedOrders);


            // Visar totalt antal Failed-ordrar från SQL.
            _meter.CreateObservableGauge (
                "itotshop.orders.failed",
                () => _failedOrders);


            // Visar totalt antal producerade enheter från SQL.
            _meter.CreateObservableGauge (
                "itotshop.units.produced",
                () => _producedUnits);


            // Visar aktuell PLC-anslutning.
            // 1 = ansluten
            // 0 = frånkopplad
            _meter.CreateObservableGauge (
                "itotshop.plc.connected",
                () => _plcConnected);


            // Visar aktuell produktionsstatus från PLC.
            // 0 = Idle
            // 1 = Processing
            // 2 = Completed
            // 3 = Fault
            _meter.CreateObservableGauge (
                "itotshop.plc.production_status",
                () => _productionStatus);


            // Visar producerad kvantitet för aktuell order.
            _meter.CreateObservableGauge (
                "itotshop.plc.produced_quantity",
                () => _producedQuantity);
            }


        // Uppdaterar totalsiffrorna med data från SQL.
        public void UpdateOrderStatistics (
            int completedOrders,
            int failedOrders,
            long producedUnits )
            {
            _completedOrders = completedOrders;
            _failedOrders = failedOrders;
            _producedUnits = producedUnits;
            }


        // Registrerar ett kommunikationsfel mot PLC.
        public void RecordPlcCommunicationFailure ()
            {
            _plcCommunicationFailures.Add (1);
            }


        // Uppdaterar live-status från PLC.
        public void UpdatePlcState (
            int productionStatus,
            int producedQuantity )
            {
            _plcConnected = 1;
            _productionStatus = productionStatus;
            _producedQuantity = producedQuantity;
            }


        // Markerar att PLC inte går att nå.
        public void SetPlcDisconnected ()
            {
            _plcConnected = 0;
            }
        }
    }