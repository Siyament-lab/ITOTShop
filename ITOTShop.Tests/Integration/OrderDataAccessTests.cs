using ITOTShop.Integration.Data;
using ITOTShop.Integration.DataAccess;
using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Models;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.Tests.Integration
    {
    public class OrderDataAccessTests
        {
        //****Test 1 **** Äldsta Order under behandling
        [Fact]
        public async Task Test1_GetNextOrderToProcessAsync_ReturnsOldestPendingOrder ()
            {
            // Arrange
            // Skapar en unik InMemory-databas endast för detta test.
            var options = new DbContextOptionsBuilder<IntegrationDbContext> ()
                .UseInMemoryDatabase (
                    databaseName: Guid.NewGuid ().ToString ())
                .Options;

            await using var context =
                new IntegrationDbContext (options);


            // Skapar två Pending-ordrar med olika CreatedAt.
            var firstOrder = new IntegrateOrder
                {
                Id = 1,
                CustomerId = 1,
                ProductName = "Product A",
                Quantity = 5,
                UnitPrice = 100,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes (-10) //Skapat 10 min tidigare
                };

            var secondOrder = new IntegrateOrder
                {
                Id = 2,
                CustomerId = 1,
                ProductName = "Product B",
                Quantity = 3,
                UnitPrice = 200,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow // skapat nu
                };


            await context.Orders.AddRangeAsync (
                firstOrder,
                secondOrder);

            await context.SaveChangesAsync ();


            var orderDataAccess =
                new OrderDataAccess (context);


            // Act
            // Hämtar nästa beställning under behandling.
            var result =
                await orderDataAccess.GetNextOrderToProcessAsync ();


            // Assert
            // 1. Kontrollerar att en order hittades.
            Assert.NotNull (result);

            // 2. Den äldsta beställning under behandling ska hämtas först.
            Assert.Equal (
                firstOrder.Id,
                result.Id);

            Assert.Equal (
                "Product A",
                result.ProductName);
            }
        //**** Test 2 **** statusuppdatering på en beställning
        [Fact]
        public async Task Test2_UpdateStatusAsync_ChangesOrderStatus ()
            {
            // Arrange
            // Skapar en unik InMemory-databas endast för detta test.
            var options = new DbContextOptionsBuilder<IntegrationDbContext> ()
                .UseInMemoryDatabase (
                    databaseName: Guid.NewGuid ().ToString ())
                .Options;

            await using var context =
                new IntegrationDbContext (options);


            // Skapar en order som från början har status Pending.
            var order = new IntegrateOrder
                {
                Id = 1,
                CustomerId = 1,
                ProductName = "Product A",
                Quantity = 5,
                UnitPrice = 100,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
                };

            await context.Orders.AddAsync (order);
            await context.SaveChangesAsync ();


            var orderDataAccess =
                new OrderDataAccess (context);

            // Assert - startstatus
            // Kontrollerar att ordern verkligen börjar som Pending.
            Assert.Equal (
                OrderStatus.Pending,
                order.Status);

            // Act
            // Ändrar orderns status från Pending till Processing.
            await orderDataAccess.UpdateStatusAsync (
                order.Id,
                OrderStatus.Processing);


            // Assert
            // Hämtar ordern igen direkt från testdatabasen.
            var updatedOrder =
                await context.Orders.FindAsync (order.Id);

            Assert.NotNull (updatedOrder);

            // Kontrollerar att status faktiskt ändrades.
            Assert.Equal (
                OrderStatus.Processing,
                updatedOrder.Status);

            // Kontrollerar att UpdatedAt också sattes vid uppdateringen.
            Assert.NotNull (updatedOrder.UpdatedAt);
            }
        //**** Test 3**** Ignorera beställningar som inte är under behandling
        [Fact]
        public async Task Test3_GetNextOrderToProcessAsync_IgnoresNonPendingOrders ()
            {
            // Arrange
            // Skapar en unik InMemory-databas endast för detta test.
            var options = new DbContextOptionsBuilder<IntegrationDbContext> ()
                .UseInMemoryDatabase (
                    databaseName: Guid.NewGuid ().ToString ())
                .Options;

            await using var context =
                new IntegrationDbContext (options);


            // Den här ordern är äldst men redan färdigbehandlad
            var completedOrder = new IntegrateOrder
                {
                Id = 1,
                CustomerId = 1,
                ProductName = "Completed Product",
                Quantity = 5,
                UnitPrice = 100,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddMinutes (-10)
                };


            // Den här ordern är Pending och ska därför hämtas.
            var pendingOrder = new IntegrateOrder
                {
                Id = 2,
                CustomerId = 1,
                ProductName = "Pending Product",
                Quantity = 3,
                UnitPrice = 200,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
                };


            await context.Orders.AddRangeAsync (
                completedOrder,
                pendingOrder);

            await context.SaveChangesAsync ();


            var orderDataAccess =
                new OrderDataAccess (context);


            // Act
            // Försöker hämta nästa order som ska behandlas.
            var result =
                await orderDataAccess.GetNextOrderToProcessAsync ();


            // Assert
            Assert.NotNull (result);

            // Kontrollerar att Pending-ordern hämtades.
            Assert.Equal (
                pendingOrder.Id,
                result.Id);

            // Kontrollerar också att den returnerade ordern verkligen är Pending.
            Assert.Equal (
                OrderStatus.Pending,
                result.Status);
            }

        //**** Test 4**** Inga behandlingsbara ordrar
        [Fact]
        public async Task Test4_GetNextOrderToProcessAsync_NoProcessableOrders_ReturnsNull ()
            {
            // Arrange
            // Skapar en unik InMemory-databas endast för detta test.
            var options = new DbContextOptionsBuilder<IntegrationDbContext> ()
                .UseInMemoryDatabase (
                    databaseName: Guid.NewGuid ().ToString ())
                .Options;

            await using var context =
                new IntegrationDbContext (options);


            // Skapar endast ordrar som inte är Pending.
            var completedOrder = new IntegrateOrder
                {
                Id = 1,
                CustomerId = 1,
                ProductName = "Completed Product",
                Quantity = 5,
                UnitPrice = 100,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow
                };

            var failedOrder = new IntegrateOrder
                {
                Id = 2,
                CustomerId = 1,
                ProductName = "Failed Product",
                Quantity = 25,
                UnitPrice = 100,
                Status = OrderStatus.Failed,
                CreatedAt = DateTime.UtcNow
                };


            await context.Orders.AddRangeAsync (
                completedOrder,
                failedOrder);

            await context.SaveChangesAsync ();


            var orderDataAccess =
                new OrderDataAccess (context);


            // Act
            // Försöker hämta nästa underbehandling-order.
            var result =
                await orderDataAccess.GetNextOrderToProcessAsync ();


            // Assert
            // Ingen Pending-order finns och resultatet ska därför vara null.
            Assert.Null (result);
            }

        // **** Test 5**** Status färdig
        [Fact]
        public async Task Test5_UpdateStatusAsync_ProcessingToCompleted_ChangesStatus ()
            {
            // Arrange
            // Skapar en unik InMemory-databas endast för detta test.
            var options = new DbContextOptionsBuilder<IntegrationDbContext> ()
                .UseInMemoryDatabase (
                    databaseName: Guid.NewGuid ().ToString ())
                .Options;

            await using var context =
                new IntegrationDbContext (options);


            // Skapar en order som redan befinner sig i Processing.
            var order = new IntegrateOrder
                {
                Id = 1,
                CustomerId = 1,
                ProductName = "Completed Test",
                Quantity = 5,
                UnitPrice = 100,
                Status = OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow
                };


            await context.Orders.AddAsync (order);
            await context.SaveChangesAsync ();


            var orderDataAccess =
                new OrderDataAccess (context);


            // Assert - startstatus
            // Kontrollerar att ordern verkligen börjar som Processing.
            Assert.Equal (
                OrderStatus.Processing,
                order.Status);


            // Act
            // Simulerar att produktionen har slutförts korrekt.
            await orderDataAccess.UpdateStatusAsync (
                order.Id,
                OrderStatus.Completed);


            // Assert - slutstatus
            var updatedOrder =
                await context.Orders.FindAsync (order.Id);

            Assert.NotNull (updatedOrder);

            // Kontrollerar att status ändrades till Completed.
            Assert.Equal (
                OrderStatus.Completed,
                updatedOrder.Status);

            // Kontrollerar att UpdatedAt sattes när status ändrades.
            Assert.NotNull (updatedOrder.UpdatedAt);
            }

        //**** Test 6 **** Status misslyckad
        [Fact]
        public async Task Test6_UpdateStatusAsync_ProcessingToFailed_ChangesStatus ()
            {
            // Arrange
            // Skapar en unik InMemory-databas endast för detta test.
            var options = new DbContextOptionsBuilder<IntegrationDbContext> ()
                .UseInMemoryDatabase (
                    databaseName: Guid.NewGuid ().ToString ())
                .Options;

            await using var context =
                new IntegrationDbContext (options);


            // Skapar en order som redan befinner sig i Processing.
            var order = new IntegrateOrder
                {
                Id = 1,
                CustomerId = 1,
                ProductName = "Failed Test",
                Quantity = 25,
                UnitPrice = 100,
                Status = OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow
                };


            await context.Orders.AddAsync (order);
            await context.SaveChangesAsync ();


            var orderDataAccess =
                new OrderDataAccess (context);


            // Assert - startstatus
            // Kontrollerar att ordern verkligen börjar som Processing.
            Assert.Equal (
                OrderStatus.Processing,
                order.Status);


            // Act
            // Simulerar att PLC:n har rapporterat ett produktionsfel.
            await orderDataAccess.UpdateStatusAsync (
                order.Id,
                OrderStatus.Failed);


            // Assert - slutstatus
            var updatedOrder =
                await context.Orders.FindAsync (order.Id);

            Assert.NotNull (updatedOrder);

            // Kontrollerar att status ändrades till Failed.
            Assert.Equal (
                OrderStatus.Failed,
                updatedOrder.Status);

            // Kontrollerar att UpdatedAt sattes när status ändrades.
            Assert.NotNull (updatedOrder.UpdatedAt);
            }
        }
    }