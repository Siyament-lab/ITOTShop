using ITOTShop.Integration.DTOs;
using ITOTShop.Integration.Enums;
using ITOTShop.Integration.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.Integration.Interfaces
    {
    public interface IOrderDataAccess
        {
        //Hämtar order som står på "pending" och inte har gått vidare till OT
        Task<IntegrateOrder?> GetNextOrderToProcessAsync (
            CancellationToken cancellationToken = default);

        //Uppdaterar status på ordern efter den är hämtat fr orderboken i db
        Task UpdateStatusAsync (
            int orderId,
            OrderStatus Status,
            CancellationToken cancellationToken = default );

        Task<OrderStatisticsDto> GetStatisticsAsync (
        CancellationToken cancellationToken = default );
        }
    }
