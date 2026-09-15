
using Microsoft.EntityFrameworkCore;
using ITOTShop.Integration.Data;
using ITOTShop.Integration.Interfaces;
using ITOTShop.Integration.Models;
using System;
using System.Collections.Generic;
using System.Text;
using ITOTShop.Integration.Enums;

namespace ITOTShop.Integration.DataAccess
    {
    public class OrderDataAccess: IOrderDataAccess
        {
        private readonly IntegrationDbContext _Context;
        public OrderDataAccess ( IntegrationDbContext context )
            {
            _Context = context;
            }
        public async Task<List<IntegrateOrder>> GetPendingOrdersAsync (
            CancellationToken cancellationToken = default )
            {
            // Hämtar alla orderar som väntar på att behandlas
            return await _Context.Orders
                .AsNoTracking () //Tillfälligt användning för att minska risken att råka ändra något i db.
                .Where (o => o.Status == Enums.OrderStatus.Pending)
                .ToListAsync (cancellationToken);
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
        }
    }
