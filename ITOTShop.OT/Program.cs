using ITOTShop.OT.Services;

namespace ITOTShop.OT
    {
    internal class Program
        {
        static async Task Main ( string[] args )
            {
            // Håller samma Modbus-anslutning mot OpenPLC
            // under hela programmets körning.
            using var modbusMonitorService =
                new ModbusMonitorService ();


            // Hanterar statistik för lyckade och misslyckade ordrar.
            var productionStatisticsService =
                new PlcStatisticsService ();


            Console.Clear ();


            while (true)
                {
                try
                    {
                    // Läser aktuell produktionsstatus från OpenPLC.
                    var productionState =
                        modbusMonitorService.ReadProductionState ();


                    // Översätter PLC-status från siffra
                    // till tydligare text i OT-konsolen.
                    string statusText =
                        productionState.ProductionStatus switch
                            {
                                0 => "Idle",
                                1 => "Processing",
                                2 => "Completed",
                                3 => "Fault",
                                _ => "Unknown"
                                };


                    // Uppdaterar statistik utifrån aktuell PLC-status.
                    productionStatisticsService.UpdateStatistics (
                        productionState);


                    // Flyttar markören tillbaka till början av konsolen
                    // för att minska flimmer vid live-uppdateringen.
                    Console.SetCursorPosition (0, 0);


                    Console.WriteLine (
                        "--- ITOTShop OT Monitor ---");

                    Console.WriteLine ();


                    // ================= LIVE PRODUCTION =================

                    Console.WriteLine (
                        "--- Live Production ---");

                    Console.WriteLine (
                        $"Order ID:             {productionState.OrderId}        ");

                    Console.WriteLine (
                        $"Requested Quantity:   {productionState.RequestedQuantity}        ");

                    Console.WriteLine (
                        $"Production Status:    {statusText}        ");

                    Console.WriteLine (
                        $"Produced Quantity:    {productionState.ProducedQuantity}        ");

                    Console.WriteLine (
                        $"Fault Code:           {productionState.FaultCode}        ");

                    Console.WriteLine (
                        $"Production Complete:  {productionState.ProductionComplete}        ");

                    Console.WriteLine (
                        $"Production Fault:     {productionState.ProductionFault}        ");


                    Console.WriteLine ();


                    // ================= PRODUCTION TOTALS =================

                    Console.WriteLine (
                        "--- Production Totals ---");

                    Console.WriteLine (
                        $"Succeeded Orders:     {productionStatisticsService.SucceededOrders}        ");

                    Console.WriteLine (
                        $"Failed Orders:        {productionStatisticsService.FailedOrders}        ");

                    Console.WriteLine (
                        $"Total Orders:         {productionStatisticsService.TotalOrders}        ");


                    Console.WriteLine ();

                    Console.WriteLine (
                        "Live update running...");


                    // Uppdaterar live-status fyra gånger per sekund.
                    await Task.Delay (
                        TimeSpan.FromMilliseconds (250));
                    }
                catch (Exception ex)
                    {
                    // Om OpenPLC inte går att nå behåller vi
                    // den statistik som redan har samlats in.
                    Console.SetCursorPosition (0, 0);

                    Console.WriteLine (
                        "--- ITOTShop OT Monitor ---");

                    Console.WriteLine ();

                    Console.WriteLine (
                        "OpenPLC connection unavailable.");

                    Console.WriteLine (
                        $"Error: {ex.Message}");

                    Console.WriteLine ();

                    Console.WriteLine (
                        "--- Production Totals ---");

                    Console.WriteLine (
                        $"Succeeded Orders:     {productionStatisticsService.SucceededOrders}");

                    Console.WriteLine (
                        $"Failed Orders:        {productionStatisticsService.FailedOrders}");

                    Console.WriteLine (
                        $"Total Orders:         {productionStatisticsService.TotalOrders}");


                    // Väntar innan nästa anslutningsförsök.
                    await Task.Delay (
                        TimeSpan.FromSeconds (1));
                    }
                }
            }
        }
    }