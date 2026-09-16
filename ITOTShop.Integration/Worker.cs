using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Interfaces;
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

        protected override async Task ExecuteAsync (
            CancellationToken stoppingToken )
            {
            _logger.LogInformation ("Integration service started.");

            //// Tillfälligt Modbus-test mot OpenPLC
            //using (var testScope = _scopeFactory.CreateScope ())
            //    {
            //    var modbusService =
            //        testScope.ServiceProvider.GetRequiredService<ModbusService> ();

            //    await modbusService.SendTestOrderAsync ();
            //    }

            while (!stoppingToken.IsCancellationRequested)
                {
                try
                    {
                    // Skapar ett nytt scope för varje körning eftersom DbContext är Scoped
                    using var scope = _scopeFactory.CreateScope ();

                    var orderDataAccess =
                        scope.ServiceProvider.GetRequiredService<IOrderDataAccess> ();
                    var modbusService =
                        scope.ServiceProvider.GetRequiredService<ModbusService> ();

                    // Hämtar endast ordrar som väntar på behandling
                    var pendingOrders =
                        await orderDataAccess.GetPendingOrdersAsync (stoppingToken);

                    if (pendingOrders.Count == 0)
                        {
                        _logger.LogInformation ("No pending orders found.");
                        }
                    else
                        {
                        _logger.LogInformation (
                            "{OrderCount} pending order(s) found.",
                            pendingOrders.Count);

                        foreach (var order in pendingOrders)
                            {
                            _logger.LogInformation (
                                "Order {OrderId} | Product: {ProductName} | Quantity: {Quantity} | Unit Price: {UnitPrice} | Status: {Status}",
                                order.Id,
                                order.ProductName,
                                order.Quantity,
                                order.UnitPrice,
                                order.Status);

                            // Skickar ordern till OpenPLC
                            await modbusService.SendOrderAsync (
                                (ushort) order.Id,
                                (ushort) order.Quantity);

                            // Markerar ordern som Processing efter att den skickats till PLC:n
                            await orderDataAccess.UpdateStatusAsync (
                                order.Id,
                                OrderStatus.Processing,
                                stoppingToken);

                            _logger.LogInformation (
                                "Order {OrderId} sent to PLC and changed to Processing.",
                                order.Id);
                            }
                        }
                    }
                catch (Exception ex)
                    {
                    // Ett fel ska inte stoppa hela integrationsprogrammet
                    _logger.LogError (
                        ex,
                        "An error occurred while reading orders from the database.");
                    }

                // Väntar 5 sekunder innan databasen kontrolleras igen
                await Task.Delay (
                    TimeSpan.FromSeconds (5),
                    stoppingToken);
                }
            }
        }
    }