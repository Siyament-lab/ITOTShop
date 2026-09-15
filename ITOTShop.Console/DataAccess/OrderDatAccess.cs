using ITOTShop.OrderConsole.Data;
using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.OrderConsole.DataAccess
    {
    public class OrderDataAccess: IOrderDataAccess
        {
        private readonly AppDbContext _context;

        // Tar emot AppDbContext via dependency injection
        public OrderDataAccess ( AppDbContext context )
            {
            _context = context;
            }
        //Hämtar alla ordrar från databasen
        public async Task<List<Order>> GetAllAsync ()
            {
            return await _context.Orders.ToListAsync ();
            }

        // Hämtar ordrar tillhörande specifik kund baserat på kundens ID
        public async Task<List<Order>> GetByCustomerIdAsync ( int customerId )
            {
            return await _context.Orders
                .Where (o => o.CustomerId == customerId)
                .ToListAsync ();
            }

        // Lägger till en order och sparar ändringen i databasen
        public async Task AddAsync ( Order order )
            {
            await _context.Orders.AddAsync (order);
            await _context.SaveChangesAsync ();
            }
        }
    }