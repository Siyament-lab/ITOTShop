using ITOTShop.OT.Models;
using ITOTShop.OT.Services;

namespace ITOTShop.Tests.OT
    {
    public class PlcStatisticsServiceTests
        {
        //**** Test 1 **** räkna upp färdiga ordrar
        [Fact]
        public void Test1_UpdateStatistics_CompletedOrder_IncreasesSucceededOrders ()
            {
            // Arrange
            var plcStatisticsService =
                new PlcStatisticsService ();

            var productionState = new ProductionState
                {
                OrderId = 1,
                ProductionStatus = 2,
                ProductionComplete = true,
                ProductionFault = false
                };


            // Assert - startvärden
            // Kontrollerar att statistiken börjar på 0.
            Assert.Equal (
                0,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                0,
                plcStatisticsService.FailedOrders);


            // Act
            // Simulerar att PLC:n rapporterar en färdig order.
            plcStatisticsService.UpdateStatistics (
                productionState);


            // Assert - slutvärden
            // Kontrollerar att ordern räknades som lyckad.
            Assert.Equal (
                1,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                0,
                plcStatisticsService.FailedOrders);

            Assert.Equal (
                1,
                plcStatisticsService.TotalOrders);
            }

        //**** Test 2 **** Räkna upp misslyckade ordrar
        [Fact]
        public void Test2_UpdateStatistics_FaultOrder_IncreasesFailedOrders ()
            {
            // Arrange
            var plcStatisticsService =
                new PlcStatisticsService ();

            var productionState = new ProductionState
                {
                OrderId = 2,
                ProductionStatus = 3,
                ProductionComplete = false,
                ProductionFault = true,
                FaultCode = 3
                };


            // Assert - startvärden
            Assert.Equal (
                0,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                0,
                plcStatisticsService.FailedOrders);


            // Act
            // Simulerar att PLC:n rapporterar ett produktionsfel.
            plcStatisticsService.UpdateStatistics (
                productionState);


            // Assert - slutvärden
            // Kontrollerar att ordern räknades som misslyckad.
            Assert.Equal (
                0,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                1,
                plcStatisticsService.FailedOrders);

            Assert.Equal (
                1,
                plcStatisticsService.TotalOrders);
            }

        //**** Test 3 **** Order under behandling skall ej räknas som lyckad/misslyckad
        [Fact]
        public void Test3_UpdateStatistics_ProcessingOrder_DoesNotIncreaseStatistics ()
            {
            // Arrange
            var plcStatisticsService =
                new PlcStatisticsService ();

            var productionState = new ProductionState
                {
                OrderId = 3,
                ProductionStatus = 1,
                ProductionComplete = false,
                ProductionFault = false
                };


            // Act
            // Simulerar en order som fortfarande produceras.
            plcStatisticsService.UpdateStatistics (
                productionState);


            // Assert
            // En Processing-order ska inte räknas som färdig eller misslyckad.
            Assert.Equal (
                0,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                0,
                plcStatisticsService.FailedOrders);

            Assert.Equal (
                0,
                plcStatisticsService.TotalOrders);
            }

        //**** Test 4 **** Samma lyckade order ska inte räknas upp 2 gånger
        [Fact]
        public void Test4_UpdateStatistics_SameCompletedOrder_DoesNotCountTwice ()
            {
            // Arrange
            var plcStatisticsService =
                new PlcStatisticsService ();

            var productionState = new ProductionState
                {
                OrderId = 4,
                ProductionStatus = 2,
                ProductionComplete = true,
                ProductionFault = false
                };


            // Act
            // Samma PLC-status läses flera gånger av OT-monitorn.
            plcStatisticsService.UpdateStatistics (
                productionState);

            plcStatisticsService.UpdateStatistics (
                productionState);

            plcStatisticsService.UpdateStatistics (
                productionState);


            // Assert
            // Samma OrderId ska bara räknas en gång.
            Assert.Equal (
                1,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                0,
                plcStatisticsService.FailedOrders);

            Assert.Equal (
                1,
                plcStatisticsService.TotalOrders);
            }

        //**** Test 5 ****Samma misslyckade ordrar ska inte räknas dubbelt
        [Fact]
        public void Test5_UpdateStatistics_SameFailedOrder_DoesNotCountTwice ()
            {
            // Arrange
            var plcStatisticsService =
                new PlcStatisticsService ();

            var productionState = new ProductionState
                {
                OrderId = 5,
                ProductionStatus = 3,
                ProductionComplete = false,
                ProductionFault = true,
                FaultCode = 3
                };


            // Act
            // Samma Fault-status läses flera gånger från PLC:n.
            plcStatisticsService.UpdateStatistics (
                productionState);

            plcStatisticsService.UpdateStatistics (
                productionState);

            plcStatisticsService.UpdateStatistics (
                productionState);


            // Assert
            // Samma misslyckade order ska bara räknas en gång.
            Assert.Equal (
                0,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                1,
                plcStatisticsService.FailedOrders);

            Assert.Equal (
                1,
                plcStatisticsService.TotalOrders);
            }

        //**** Test 6 **** Räkna upp alla lagda ordrar (lyckade och misslyckade)
        [Fact]
        public void Test6_UpdateStatistics_MultipleOrders_CalculatesCorrectTotals ()
            {
            // Arrange
            var plcStatisticsService =
                new PlcStatisticsService ();


            var completedOrder1 = new ProductionState
                {
                OrderId = 10,
                ProductionStatus = 2,
                ProductionComplete = true
                };

            var completedOrder2 = new ProductionState
                {
                OrderId = 11,
                ProductionStatus = 2,
                ProductionComplete = true
                };

            var failedOrder = new ProductionState
                {
                OrderId = 12,
                ProductionStatus = 3,
                ProductionFault = true,
                FaultCode = 3
                };


            // Act
            plcStatisticsService.UpdateStatistics (
                completedOrder1);

            plcStatisticsService.UpdateStatistics (
                completedOrder2);

            plcStatisticsService.UpdateStatistics (
                failedOrder);


            // Assert
            // Två lyckade och en misslyckad order ska ge totalt tre ordrar.
            Assert.Equal (
                2,
                plcStatisticsService.SucceededOrders);

            Assert.Equal (
                1,
                plcStatisticsService.FailedOrders);

            Assert.Equal (
                3,
                plcStatisticsService.TotalOrders);
            }
        }
    }