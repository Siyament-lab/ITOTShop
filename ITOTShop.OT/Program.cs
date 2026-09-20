using ITOTShop.OT.Services;

namespace ITOTShop.OT
    {
    internal class Program
        {
        static async Task Main ( string[] args )
            {
            // Skapar ModbusService och håller samma anslutning
            // mot OpenPLC under hela programmets körning.
            using var modbusMonitorService =
                new ModbusMonitorService ();


            // Räknar antal färdiga och misslyckade ordrar
            // under tiden OT-programmet körs.
            int succeededOrders = 0;
            int failedOrders = 0;


            // Sparar senaste OrderId som redan har räknats.
            // Detta förhindrar att samma order räknas flera gånger
            // när OT-programmet uppdaterar status kontinuerligt.
            ushort? lastSucceededOrderId = null;
            ushort? lastFailedOrderId = null;


            Console.Clear ();


            while (true)
                {
                try
                    {
                    // Läser aktuell status från OpenPLC.
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


                    // Kontrollerar om en ny order har blivit färdig.
                    if ((productionState.ProductionStatus == 2 ||
                         productionState.ProductionComplete) &&
                        productionState.OrderId != 0 &&
                        lastSucceededOrderId != productionState.OrderId)
                        {
                        succeededOrders++;

                        lastSucceededOrderId =
                            productionState.OrderId;
                        }


                    // Kontrollerar om en ny order har fått produktionsfel.
                    if ((productionState.ProductionStatus == 3 ||
                         productionState.ProductionFault) &&
                        productionState.OrderId != 0 &&
                        lastFailedOrderId != productionState.OrderId)
                        {
                        failedOrders++;

                        lastFailedOrderId =
                            productionState.OrderId;
                        }


                    // Flyttar markören tillbaka till början av konsolen.
                    // Vi slipper Console.Clear() varje gång och minskar flimmer.
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
                        $"Succeeded Orders:     {succeededOrders}        ");

                    Console.WriteLine (
                        $"Failed Orders:        {failedOrders}        ");

                    Console.WriteLine (
                        $"Total Orders:         {succeededOrders + failedOrders}        ");


                    Console.WriteLine ();

                    Console.WriteLine (
                        "Live update running...");


                    // Uppdaterar snabbare än tidigare så att korta
                    // Completed/Fault-statusar lättare fångas.
                    await Task.Delay (
                        TimeSpan.FromMilliseconds (250));
                    }
                catch (Exception ex)
                    {
                    // Om OpenPLC tillfälligt inte går att nå
                    // behåller vi tidigare totalsiffror.
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
                        $"Succeeded Orders:     {succeededOrders}");

                    Console.WriteLine (
                        $"Failed Orders:        {failedOrders}");

                    Console.WriteLine (
                        $"Total Orders:         {succeededOrders + failedOrders}");


                    // Väntar innan OT-programmet försöker läsa PLC:n igen.
                    await Task.Delay (
                        TimeSpan.FromSeconds (1));
                    }
                }
            }
        }
    }