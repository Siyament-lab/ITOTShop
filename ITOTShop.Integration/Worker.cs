using ITOTShop.Integration.DTOs;
using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Interfaces;
using ITOTShop.Integration.Models;
using ITOTShop.Integration.Monitoring;
using ITOTShop.Integration.Services;

namespace ITOTShop.Integration
    {
    public class Worker: BackgroundService
        {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IntegrationMetrics _metrics;


        public Worker (
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IntegrationMetrics metrics )
            {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _metrics = metrics;
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
                    // Skapar ett nytt scope eftersom DbContext är Scoped.
                    using var scope =
                        _scopeFactory.CreateScope ();


                    // Hämtar DataAccess för orderdatabasen.
                    var orderDataAccess =
                        scope.ServiceProvider
                            .GetRequiredService<IOrderDataAccess> ();


                    // Hämtar ModbusService för kommunikationen med OpenPLC.
                    var modbusService =
                        scope.ServiceProvider
                            .GetRequiredService<ModbusService> ();


                    // Hämtar senaste permanenta totalsiffrorna från SQL.
                    // Detta gör att metrics återställs korrekt
                    // även om Integration startas om.
                    await RefreshOrderStatisticsAsync (
                        orderDataAccess,
                        stoppingToken);


                    // Hämtar endast EN Pending-order åt gången.
                    var order =
                        await orderDataAccess
                            .GetNextPendingOrderAsync (
                                stoppingToken);


                    // Om ingen Pending-order finns kontrolleras ändå
                    // PLC-status så att Grafana visar aktuell anslutning
                    // och produktionsstatus även när systemet är Idle.
                    if (order == null)
                        {
                        UpdateIdlePlcMetrics (
                            modbusService);


                        _logger.LogInformation (
                            "No pending orders found.");


                        await Task.Delay (
                            TimeSpan.FromSeconds (5),
                            stoppingToken);

                        continue;
                        }


                    // Behandlar hela ordern innan nästa order får hämtas.
                    await ProcessOrderAsync (
                        order,
                        orderDataAccess,
                        modbusService,
                        stoppingToken);
                    }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                    {
                    // Programmet håller på att stoppas normalt.
                    _logger.LogInformation (
                        "Integration service stopping.");

                    break;
                    }
                catch (Exception ex)
                    {
                    // Ett oväntat fel ska inte stoppa hela integrationsprogrammet.
                    // Endast själva felmeddelandet loggas för tydligare konsol.
                    _logger.LogError (
                        "Integration error: {ErrorMessage}",
                        ex.Message);


                    // Väntar innan ett nytt försök görs.
                    try
                        {
                        await Task.Delay (
                            TimeSpan.FromSeconds (5),
                            stoppingToken);
                        }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                        {
                        break;
                        }
                    }
                }
            }


        // Behandlar en order från att den hämtas från SQL
        // tills PLC:n rapporterar Completed eller ett riktigt Fault.
        //
        // Kommunikationsproblem gör INTE ordern Failed.
        // Integration väntar i stället tills PLC:n fungerar igen.
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


            // Försöker skicka ordern till PLC:n.
            // Om PLC:n är offline ligger ordern kvar som Pending
            // och samma order försöks igen tills kommunikationen fungerar.
            await SendOrderToPlcUntilSuccessAsync (
                order,
                modbusService,
                stoppingToken);


            // SQL-status ändras till Processing först när
            // ordern faktiskt har skickats till PLC:n.
            await orderDataAccess.UpdateStatusAsync (
                order.Id,
                OrderStatus.Processing,
                stoppingToken);


            _logger.LogInformation (
                "Order {OrderId} sent to PLC. Waiting for production result...",
                order.Id);


            // Räknar kommunikationsfel under pågående produktion.
            int plcReadFailures = 0;


            // Används för att veta om PLC-kommunikationen
            // tidigare har varit nere under denna order.
            bool communicationWasLost = false;


            // Används endast för varningsloggning.
            // Ordern blir INTE Failed om produktionen tar lång tid.
            var nextLongRunningWarning =
                DateTime.UtcNow.AddSeconds (30);


            // Väntar på samma order tills PLC:n rapporterar
            // Completed eller ett riktigt produktionsfel.
            while (!stoppingToken.IsCancellationRequested)
                {
                ProductionStateDto productionState;


                try
                    {
                    // Läser aktuell produktionsstatus från OpenPLC.
                    productionState =
                        modbusService.ReadProductionState ();


                    // Uppdaterar live-metrics med aktuell PLC-status.
                    // En lyckad läsning betyder även att PLC:n är ansluten.
                    _metrics.UpdatePlcState (
                        (int) productionState.Status,
                        productionState.ProducedQuantity);


                    // Om vi tidigare tappade kontakten men nu lyckas läsa igen
                    // har kommunikationen återställts.
                    if (communicationWasLost)
                        {
                        _logger.LogInformation (
                            "PLC communication restored for Order {OrderId}.",
                            order.Id);
                        }


                    // Kommunikationsräknaren återställs
                    // efter en lyckad läsning.
                    plcReadFailures = 0;
                    }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                    {
                    // Programmet håller på att stoppas normalt.
                    throw;
                    }
                catch (Exception ex)
                    {
                    plcReadFailures++;
                    communicationWasLost = true;


                    // Markerar PLC som frånkopplad i metrics.
                    _metrics.SetPlcDisconnected ();


                    // Registrerar ett kommunikationsfel mot PLC.
                    _metrics.RecordPlcCommunicationFailure ();


                    // Ordern är fortfarande Processing.
                    // Kommunikationsproblem ska inte markera produktionen som Failed.
                    _logger.LogWarning (
                        "PLC communication unavailable for Order {OrderId}. Retry {Attempt}. Error: {ErrorMessage}",
                        order.Id,
                        plcReadFailures,
                        ex.Message);


                    // Väntar innan samma PLC-status försöks läsas igen.
                    await Task.Delay (
                        TimeSpan.FromSeconds (2),
                        stoppingToken);

                    continue;
                    }


                // Om kommunikationen har varit nere och PLC:n efter återkomst
                // står i Idle betyder det i vår simulator att PLC Runtime
                // har tappat den pågående produktionen.
                //
                // Samma order skickas då på nytt.
                if (communicationWasLost &&
                    productionState.Status == ProductionStatus.Idle &&
                    !productionState.ProductionComplete &&
                    !productionState.ProductionFault)
                    {
                    _logger.LogWarning (
                        "PLC returned to Idle after communication loss. Resending Order {OrderId}.",
                        order.Id);


                    // Skickar samma order igen.
                    // SQL-status ligger fortfarande kvar som Processing.
                    await SendOrderToPlcUntilSuccessAsync (
                        order,
                        modbusService,
                        stoppingToken);


                    communicationWasLost = false;

                    continue;
                    }


                // Kommunikationen fungerar igen och PLC:n har fortfarande
                // kvar produktionens status.
                communicationWasLost = false;


                // Kontrollerar om produktionen är färdig.
                if (productionState.Status == ProductionStatus.Completed ||
                    productionState.ProductionComplete)
                    {
                    // SQL-status ändras till Completed.
                    await orderDataAccess.UpdateStatusAsync (
                        order.Id,
                        OrderStatus.Completed,
                        stoppingToken);


                    // Uppdaterar totalsiffrorna från SQL efter Completed.
                    // Completed Orders och Produced Units får därmed
                    // permanenta värden som överlever omstart.
                    await RefreshOrderStatisticsAsync (
                        orderDataAccess,
                        stoppingToken);


                    _logger.LogInformation (
                        "Order {OrderId} completed. Produced quantity: {ProducedQuantity}",
                        order.Id,
                        productionState.ProducedQuantity);


                    // Återställer PLC:n innan nästa order får behandlas.
                    await ResetPlcUntilIdleAsync (
                        modbusService,
                        stoppingToken);

                    return;
                    }


                // Kontrollerar om PLC:n rapporterar ett RIKTIGT produktionsfel.
                // Först här ska SQL-ordern markeras som Failed.
                if (productionState.Status == ProductionStatus.Fault ||
                    productionState.ProductionFault)
                    {
                    // SQL-status ändras till Failed.
                    await orderDataAccess.UpdateStatusAsync (
                        order.Id,
                        OrderStatus.Failed,
                        stoppingToken);


                    // Uppdaterar totalsiffrorna från SQL efter Failed.
                    // Failed Orders får därmed ett permanent värde
                    // som överlever omstart.
                    await RefreshOrderStatisticsAsync (
                        orderDataAccess,
                        stoppingToken);


                    _logger.LogError (
                        "Order {OrderId} failed. Fault code: {FaultCode}",
                        order.Id,
                        productionState.FaultCode);


                    // Återställer PLC:n innan nästa order får behandlas.
                    await ResetPlcUntilIdleAsync (
                        modbusService,
                        stoppingToken);

                    return;
                    }


                // Om produktionen tar längre tid än normalt
                // loggas endast en varning.
                //
                // Ordern blir INTE Failed eftersom produktionen
                // fortfarande kan fortsätta.
                if (DateTime.UtcNow >= nextLongRunningWarning)
                    {
                    _logger.LogWarning (
                        "Order {OrderId} is still processing in PLC.",
                        order.Id);


                    // Nästa varning skrivs tidigast om 30 sekunder.
                    nextLongRunningWarning =
                        DateTime.UtcNow.AddSeconds (30);
                    }


                // Väntar innan PLC-status läses igen.
                await Task.Delay (
                    TimeSpan.FromSeconds (1),
                    stoppingToken);
                }
            }


        // Försöker skicka samma order till OpenPLC tills det lyckas.
        //
        // Om PLC:n är offline markeras ordern INTE som Failed.
        // Den ligger kvar och Integration väntar tills PLC:n är tillgänglig igen.
        private async Task SendOrderToPlcUntilSuccessAsync (
            IntegrateOrder order,
            ModbusService modbusService,
            CancellationToken stoppingToken )
            {
            int attempt = 0;


            while (!stoppingToken.IsCancellationRequested)
                {
                attempt++;


                try
                    {
                    // Försöker skicka OrderId och Quantity till OpenPLC.
                    await modbusService.SendOrderAsync (
                        (ushort) order.Id,
                        (ushort) order.Quantity);


                    // Om tidigare försök misslyckades
                    // meddelar vi att kommunikationen fungerar igen.
                    if (attempt > 1)
                        {
                        _logger.LogInformation (
                            "PLC connection restored. Order {OrderId} sent successfully.",
                            order.Id);
                        }


                    return;
                    }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                    {
                    // Programmet håller på att stoppas normalt.
                    throw;
                    }
                catch (Exception ex)
                    {
                    // Markerar PLC som frånkopplad i metrics.
                    _metrics.SetPlcDisconnected ();


                    // Registrerar ett kommunikationsfel mot PLC.
                    _metrics.RecordPlcCommunicationFailure ();


                    // Ordern ligger kvar som Pending om den ännu
                    // inte har kunnat skickas till PLC:n.
                    _logger.LogWarning (
                        "Could not send Order {OrderId} to PLC. Retry {Attempt}. Error: {ErrorMessage}",
                        order.Id,
                        attempt,
                        ex.Message);


                    // Väntar innan samma order försöks skickas igen.
                    await Task.Delay (
                        TimeSpan.FromSeconds (5),
                        stoppingToken);
                    }
                }


            throw new OperationCanceledException (stoppingToken);
            }


        // Återställer PLC:n efter Completed eller Fault.
        //
        // Om PLC-kommunikationen är nere väntar Integration tills
        // kommunikationen fungerar igen innan nästa order får behandlas.
        private async Task ResetPlcUntilIdleAsync (
            ModbusService modbusService,
            CancellationToken stoppingToken )
            {
            int resetAttempt = 0;


            // Försöker skicka ResetProduction tills det lyckas.
            while (!stoppingToken.IsCancellationRequested)
                {
                resetAttempt++;


                try
                    {
                    // Skickar ResetProduction till OpenPLC.
                    await modbusService.ResetProductionAsync ();

                    break;
                    }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                    {
                    throw;
                    }
                catch (Exception ex)
                    {
                    // Markerar PLC som frånkopplad i metrics.
                    _metrics.SetPlcDisconnected ();


                    // Registrerar ett kommunikationsfel mot PLC.
                    _metrics.RecordPlcCommunicationFailure ();


                    _logger.LogWarning (
                        "Could not reset PLC. Retry {Attempt}. Error: {ErrorMessage}",
                        resetAttempt,
                        ex.Message);


                    // Nästa order får inte skickas innan PLC:n är återställd.
                    await Task.Delay (
                        TimeSpan.FromSeconds (2),
                        stoppingToken);
                    }
                }


            // När resetsignalen har skickats väntar Integration
            // tills PLC:n verkligen rapporterar Idle.
            await WaitForPlcIdleAsync (
                modbusService,
                stoppingToken);
            }


        // Kontrollerar PLC-status tills PLC:n är tillbaka i Idle.
        //
        // Nästa order får inte skickas innan:
        // ProductionStatus = Idle
        // ProductionComplete = false
        // ProductionFault = false
        //
        // Om kommunikationen bryts under reset väntar vi och försöker igen.
        private async Task WaitForPlcIdleAsync (
            ModbusService modbusService,
            CancellationToken stoppingToken )
            {
            int readAttempt = 0;


            while (!stoppingToken.IsCancellationRequested)
                {
                try
                    {
                    // Läser aktuell status från OpenPLC.
                    var productionState =
                        modbusService.ReadProductionState ();


                    // Uppdaterar live-metrics även under återställningen.
                    _metrics.UpdatePlcState (
                        (int) productionState.Status,
                        productionState.ProducedQuantity);


                    // Lyckad kommunikation återställer felräknaren.
                    readAttempt = 0;


                    // PLC:n måste vara helt återställd
                    // innan nästa order får skickas.
                    if (productionState.Status == ProductionStatus.Idle &&
                        !productionState.ProductionComplete &&
                        !productionState.ProductionFault)
                        {
                        _logger.LogInformation (
                            "PLC reset completed. PLC is Idle.");

                        return;
                        }
                    }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                    {
                    throw;
                    }
                catch (Exception ex)
                    {
                    readAttempt++;


                    // Markerar PLC som frånkopplad i metrics.
                    _metrics.SetPlcDisconnected ();


                    // Registrerar ett kommunikationsfel mot PLC.
                    _metrics.RecordPlcCommunicationFailure ();


                    _logger.LogWarning (
                        "Could not read PLC status during reset. Retry {Attempt}. Error: {ErrorMessage}",
                        readAttempt,
                        ex.Message);
                    }


                // Väntar innan PLC-status kontrolleras igen.
                await Task.Delay (
                    TimeSpan.FromSeconds (1),
                    stoppingToken);
                }


            throw new OperationCanceledException (stoppingToken);
            }


        // Hämtar permanenta totalsiffror från SQL
        // och uppdaterar värdena som exponeras till Prometheus.
        //
        // Detta gör att Completed Orders, Failed Orders
        // och Produced Units inte börjar om från 0
        // när Integration startas om.
        private async Task RefreshOrderStatisticsAsync (
            IOrderDataAccess orderDataAccess,
            CancellationToken stoppingToken )
            {
            // Hämtar aktuell statistik direkt från SQL-databasen.
            var statistics =
                await orderDataAccess.GetStatisticsAsync (
                    stoppingToken);


            // Uppdaterar metrics med de permanenta
            // totalsiffrorna från databasen.
            _metrics.UpdateOrderStatistics (
                statistics.CompletedOrders,
                statistics.FailedOrders,
                statistics.ProducedUnits);
            }


        // Kontrollerar PLC-status även när det inte finns någon Pending-order.
        //
        // Detta gör att Grafana kan visa riktig live-status:
        // Online när OpenPLC går att nå.
        // Offline när OpenPLC inte går att nå.
        // ProductionStatus och ProducedQuantity hålls också uppdaterade.
        private void UpdateIdlePlcMetrics (
            ModbusService modbusService )
            {
            try
                {
                // Läser PLC-status trots att ingen order behandlas.
                var productionState =
                    modbusService.ReadProductionState ();


                // En lyckad läsning betyder att PLC:n är Online.
                _metrics.UpdatePlcState (
                    (int) productionState.Status,
                    productionState.ProducedQuantity);
                }
            catch (Exception)
                {
                // PLC:n går inte att nå.
                // Vi markerar endast anslutningen som Offline här.
                //
                // Kommunikationsfel räknas inte upp under idle-pollningen
                // eftersom ett avstängt PLC annars skulle skapa
                // ett nytt kommunikationsfel var femte sekund.
                _metrics.SetPlcDisconnected ();
                }
            }
        }
    }