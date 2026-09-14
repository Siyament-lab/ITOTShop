using ITOTShop.Console.Data;
using ITOTShop.Console.Entities;
using ITOTShop.Console.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.Console.DataAccess
    {
    public class OrderDataAccess: IOrderDataAccess
        {
        private readonly AppDbContext _context;

        // Tar emot AppDbContext via dependency injection
        public OrderDataAccess ( AppDbContext context )
            {
            _context = context;
            }

        // Hämtar samtliga ordrar från Orders-tabellen
        public async Task<List<Order>> GetAllAsync ()
            {
            return await _context.Orders.ToListAsync ();
            }

        // Lägger till en order och sparar ändringen i databasen
        public async Task AddAsync ( Order order )
            {
            await _context.Orders.AddAsync (order);
            await _context.SaveChangesAsync ();
            }
        }
    }