using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Services;

namespace ITOTShop.OrderConsole.Menu
    {
    public class OrderMenu
        {
        private readonly OrderService _orderService;

        // Tar emot OrderService via dependency injection
        public OrderMenu ( OrderService orderService )
            {
            _orderService = orderService;
            }

        // Startar huvudmenyn för orderhanteringen
        public async Task RunAsync ()
            {
            bool running = true;

            while (running)
                {
                Console.WriteLine ("\n--- ITOTShop Order Management ---");
                Console.WriteLine ("1. List orders");
                Console.WriteLine ("2. Create order");
                Console.WriteLine ("0. Exit");
                Console.Write ("Choose option: ");

                var choice = Console.ReadLine ();

                switch (choice)
                    {
                    case "1":
                        await ListOrdersAsync ();
                        break;

                    case "2":
                        await CreateOrderAsync ();
                        break;

                    case "0":
                        running = false;
                        break;

                    default:
                        Console.WriteLine ("Invalid option.");
                        break;
                    }
                }
            }

        // Hämtar och skriver ut alla ordrar från databasen
        private async Task ListOrdersAsync ()
            {
            var orders = await _orderService.GetAllOrdersAsync ();

            foreach (var order in orders)
                {
                Console.WriteLine (
                    $"Id: {order.Id}, " +
                    $"Product: {order.ProductName}, " +
                    $"Quantity: {order.Quantity}, " +
                    $"Unit price: {order.UnitPrice}, " +
                    $"Status: {order.Status}, " +
                    $"Created: {order.CreatedAt:yyyy-MM-dd-HH:mm}");
                }
            }

        // Läser in orderdata från användaren och skapar en ny order
        private async Task CreateOrderAsync ()
            {
            Console.Write ("Product name: ");
            var productName = Console.ReadLine ();

            Console.Write ("Quantity: ");
            // Läser in kvantitet och kontrollerar att det är ett giltigt heltal
            if (!int.TryParse(Console.ReadLine(), out int quantity))
                {
                Console.WriteLine ("Invalid value, please enter a valid integer!");
                return;
                }
            

            Console.Write ("Unit price: ");
            //KOnverterar inmatning till decimal
            if(!decimal.TryParse(Console.ReadLine (), out decimal unitPrice))
                {
                Console.WriteLine ("Invalid value, please enter a valid decimal number!");
                return;
                }
           

            var order = new Order
                {
                // Tillfälligt kundId tills login/users är klara att implementeras
                CustomerId = 1,

                ProductName = productName ?? string.Empty,
                Quantity = quantity,
                UnitPrice = unitPrice
                };
            //Förhindrar krasch av program vid fel inmatningar och ger användaren feedback om vad som gick fel,
            //feedback hämtas från OrderService som kastar ArgumentException vid felaktiga inmatningar.
            try
                {
                await _orderService.CreateOrderAsync (order);
                Console.WriteLine ("Order successfully created.");
                }
            catch(ArgumentException ex)
                {
                Console.WriteLine ($"Error creating order: {ex.Message}");
                }

            }
        }
    }