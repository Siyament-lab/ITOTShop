
using ITOTShop.Integration.Data;
using ITOTShop.Integration.DTOs;
using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Interfaces;
using ITOTShop.Integration.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.Integration.DataAccess
    {
    public class OrderDataAccess: IOrderDataAccess
        {
        private readonly IntegrationDbContext _Context;
        public OrderDataAccess ( IntegrationDbContext context )
            {
            _Context = context;
            }

        //Först kontrollera ordrar som finns under processing sendan ordrar som står på tur för behandling
        public async Task<IntegrateOrder?> GetNextOrderToProcessAsync (
            CancellationToken cancellationToken = default )
            {
            var processingOrder = await _Context.Orders
                .AsNoTracking ()
                .Where (o => o.Status == OrderStatus.Processing)
                .OrderBy (o => o.CreatedAt)
                .FirstOrDefaultAsync (cancellationToken);

            if (processingOrder != null )
                {
                return processingOrder;
                }
            // Hämtar den äldsta order som väntar på att behandlas
            return await _Context.Orders
                .AsNoTracking () //Tillfälligt användning för att minska risken att råka ändra något i db.
                .Where (o => o.Status == OrderStatus.Pending)
                .OrderBy (o => o.CreatedAt)
                .FirstOrDefaultAsync (cancellationToken);
                
            }
        public async Task UpdateStatusAsync (
                int orderId,
                OrderStatus status,
                CancellationToken cancellationToken = default )
            {
            // Hämtar ordern som ska uppdateras
            var order = await _Context.Orders
                .FirstOrDefaultAsync (
                    o => o.Id == orderId,
                    cancellationToken);

            if (order == null)
                {
                return;
                }

            // Uppdaterar orderns status och tidpunkt
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _Context.SaveChangesAsync (cancellationToken);
            }
        // Hämtar permanenta produktionsvärden från SQL.
        // Dessa värden överlever om Integration eller PLC startas om.
        public async Task<OrderStatisticsDto> GetStatisticsAsync (
            CancellationToken cancellationToken = default )
            {
            // Räknar alla ordrar som har slutförts.
            var completedOrders =
                await _Context.Orders
                    .CountAsync (
                        o => o.Status == OrderStatus.Completed,
                        cancellationToken);


            // Räknar alla ordrar som har misslyckats.
            var failedOrders =
                await _Context.Orders
                    .CountAsync (
                        o => o.Status == OrderStatus.Failed,
                        cancellationToken);


            // Summerar Quantity för alla lyckade ordrar.
            // Detta blir den permanenta totalproduktionen.
            var producedUnits =
                await _Context.Orders
                    .Where (
                        o => o.Status == OrderStatus.Completed)
                    .SumAsync (
                        o => (long) o.Quantity,
                        cancellationToken);


            return new OrderStatisticsDto
                {
                CompletedOrders = completedOrders,
                FailedOrders = failedOrders,
                ProducedUnits = producedUnits
                };
            }
        }
    }
