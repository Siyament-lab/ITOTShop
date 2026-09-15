
using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Interfaces;

namespace ITOTShop.OrderConsole.Services
    {
    public class OrderService
        {
        private readonly IOrderDataAccess _orderDataAccess;

        // Tar emot DataAccess-lagret via dependency injection
        public OrderService ( IOrderDataAccess orderDataAccess )
            {
            _orderDataAccess = orderDataAccess;
            }

        // Hämtar alla ordrar via DataAccess-lagret
        public async Task<List<Order>> GetAllOrdersAsync ()
            {
            return await _orderDataAccess.GetAllAsync ();
            }

        // Validerar och skapar en ny order
        public async Task CreateOrderAsync ( Order order )
            {
            // Säkerställer att produktnamnet inte är tomt
            if (string.IsNullOrWhiteSpace (order.ProductName))
                {
                throw new ArgumentException ("Product name is required.");
                }

            // Säkerställer att antal är större än 0
            if (order.Quantity <= 0)
                {
                throw new ArgumentException ("Quantity must be greater than 0.");
                }

            // Säkerställer att priset är större än 0
            if (order.UnitPrice <= 0)
                {
                throw new ArgumentException ("Unit price must be greater than 0.");
                }

            // Skickar ordern vidare till DataAccess-lagret
            await _orderDataAccess.AddAsync (order);
            }
        }
    }