using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.Integration.Interfaces
    {
    public interface IOrderDataAccess
        {
        //Hämtar orderar som inte har gått vidare till OT
        Task<List<IntegrateOrder>> GetPendingOrdersAsync (
            CancellationToken cancellationToken = default);

        //Uppdaterar status på ordern efter den är hämtat fr orderboken i db
        Task UpdateStatusAsync (
            int orderId,
            OrderStatus Status,
            CancellationToken cancellationToken = default );
        }
    }
