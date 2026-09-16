using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Interfaces;
using ITOTShop.Integration.Models;
using ITOTShop.Integration.Services;

namespace ITOTShop.Integration
    {
    public class Worker: BackgroundService
        {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker (
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory )
            {
            _logger = logger;
            _scopeFactory = scopeFactory;
            }


        // Startar integrationsprogrammet och kör hela tiden
        // så länge applikationen inte stoppas.
        protected override async Task ExecuteAsync (
            CancellationToken stoppingToken )
            {
            _logger.LogInformation (
                "Integration service started.");

            while (!stoppingToken.IsCancellationRequested)
                {
                try
                    {
                    // Skapar ett nytt scope eftersom DbContext är Scoped
                    using var scope =
                        _scopeFactory.CreateScope ();

                    // Hämtar DataAccess för orderdatabasen
                    var orderDataAccess =
                        scope.ServiceProvider
                            .GetRequiredService<IOrderDataAccess> ();

                    // Hämtar ModbusService för kommunikationen med OpenPLC
                    var modbusService =
                        scope.ServiceProvider
                            .GetRequiredService<ModbusService> ();


                    // Hämtar endast EN Pending-order åt gången
                    var order =
                        await orderDataAccess
                            .GetNextPendingOrderAsync (
                                stoppingToken);


                    // Om ingen Pending-order finns väntar Worker
                    // innan databasen kontrolleras igen
                    if (order == null)
                        {
                        _logger.LogInformation (
                            "No pending orders found.");

                        await Task.Delay (
                            TimeSpan.FromSeconds (5),
                            stoppingToken);

                        continue;
                        }


                    // Behandlar hela ordern innan nästa order får hämtas
                    await ProcessOrderAsync (
                        order,
                        orderDataAccess,
                        modbusService,
                        stoppingToken);
                    }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                    {
                    // Programmet håller på att stoppas normalt
                    _logger.LogInformation (
                        "Integration service stopping.");

                    break;
                    }
                catch (Exception ex)
                    {
                    // Ett vanligt fel ska inte stoppa hela integrationsprogrammet
                    _logger.LogError (
                        ex,
                        "An error occurred while processing an order.");

                    // Väntar innan ett nytt försök görs
                    await Task.Delay (
                        TimeSpan.FromSeconds (5),
                        stoppingToken);
                    }
                }
            }


        // Behandlar en order från att den hämtas från SQL
        // tills produktionen blir Completed eller Fault.
        // Nästa order hämtas först när denna order är färdig.
        private async Task ProcessOrderAsync (
            IntegrateOrder order,
            IOrderDataAccess orderDataAccess,
            ModbusService modbusService,
            CancellationToken stoppingToken )
            {
            _logger.LogInformation (
                "Processing Order {OrderId} | Product: {ProductName} | Quantity: {Quantity}",
                order.Id,
                order.ProductName,
                order.Quantity);


            // Skickar OrderId och Quantity till OpenPLC
            await modbusService.SendOrderAsync (
                (ushort) order.Id,
                (ushort) order.Quantity);


            // SQL-status ändras till Processing
            // efter att ordern har skickats till PLC:n
            await orderDataAccess.UpdateStatusAsync (
                order.Id,
                OrderStatus.Processing,
                stoppingToken);


            _logger.LogInformation (
                "Order {OrderId} sent to PLC. Waiting for production result...",
                order.Id);


            // Väntar här tills PLC:n säger Completed eller Fault
            while (!stoppingToken.IsCancellationRequested)
                {
                // Läser aktuell produktionsstatus från OpenPLC
                var productionState =
                    modbusService.ReadProductionState ();


                // Kontrollerar om produktionen är färdig
                if (productionState.Status == ProductionStatus.Completed ||
                    productionState.ProductionComplete)
                    {
                    // SQL-status ändras till Completed
                    await orderDataAccess.UpdateStatusAsync (
                        order.Id,
                        OrderStatus.Completed,
                        stoppingToken);


                    _logger.LogInformation (
                        "Order {OrderId} completed. Produced quantity: {ProducedQuantity}",
                        order.Id,
                        productionState.ProducedQuantity);


                    // Återställer PLC:n innan nästa order får behandlas
                    await ResetPlcAsync (
                        modbusService,
                        stoppingToken);

                    return;
                    }


                // Kontrollerar om PLC:n rapporterar ett produktionsfel
                if (productionState.Status == ProductionStatus.Fault ||
                    productionState.ProductionFault)
                    {
                    // SQL-status ändras till Failed
                    await orderDataAccess.UpdateStatusAsync (
                        order.Id,
                        OrderStatus.Failed,
                        stoppingToken);


                    _logger.LogError (
                        "Order {OrderId} failed. Fault code: {FaultCode}",
                        order.Id,
                        productionState.FaultCode);


                    // Återställer PLC:n innan nästa order får behandlas
                    await ResetPlcAsync (
                        modbusService,
                        stoppingToken);

                    return;
                    }


                // Väntar 1 sekund innan PLC-status läses igen
                await Task.Delay (
                    TimeSpan.FromSeconds (1),
                    stoppingToken);
                }
            }


        // Skickar ResetProduction till OpenPLC
        // och väntar sedan tills PLC:n verkligen är återställd.
        private static async Task ResetPlcAsync (
            ModbusService modbusService,
            CancellationToken stoppingToken )
            {
            // Skickar resetsignal till PLC:n
            await modbusService.ResetProductionAsync ();


            // Väntar tills PLC:n verkligen är tillbaka i Idle
            await WaitForPlcIdleAsync (
                modbusService,
                stoppingToken);
            }


        // Kontrollerar PLC-status tills PLC:n är tillbaka i Idle.
        // Nästa order får inte skickas innan:
        // ProductionStatus = Idle
        // ProductionComplete = false
        // ProductionFault = false
        private static async Task WaitForPlcIdleAsync (
            ModbusService modbusService,
            CancellationToken stoppingToken )
            {
            while (!stoppingToken.IsCancellationRequested)
                {
                // Läser aktuell status från OpenPLC
                var productionState =
                    modbusService.ReadProductionState ();


                // PLC:n måste vara helt återställd
                // innan nästa order får skickas
                if (productionState.Status == ProductionStatus.Idle &&
                    !productionState.ProductionComplete &&
                    !productionState.ProductionFault)
                    {
                    return;
                    }


                // Väntar en kort stund innan PLC-status kontrolleras igen
                await Task.Delay (
                    TimeSpan.FromMilliseconds (500),
                    stoppingToken);
                }
            }
        }
    }