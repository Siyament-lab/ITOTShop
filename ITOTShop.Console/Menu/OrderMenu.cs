using ITOTShop.Console.Entities;
using ITOTShop.Console.Services;

namespace ITOTShop.Console.Menu
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
                System.Console.WriteLine ("\n--- ITOTShop Order Management ---");
                System.Console.WriteLine ("1. List orders");
                System.Console.WriteLine ("2. Create order");
                System.Console.WriteLine ("0. Exit");
                System.Console.Write ("Choose option: ");

                var choice = System.Console.ReadLine ();

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
                        System.Console.WriteLine ("Invalid option.");
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
                System.Console.WriteLine (
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
            System.Console.Write ("Product name: ");
            var productName = System.Console.ReadLine ();

            System.Console.Write ("Quantity: ");
            int quantity = int.Parse (System.Console.ReadLine ()!);

            System.Console.Write ("Unit price: ");
            decimal unitPrice = decimal.Parse (System.Console.ReadLine ()!);

            var order = new Order
                {
                // Tillfälligt kundId tills login/users är klara att implementeras
                CustomerId = 1,

                ProductName = productName ?? string.Empty,
                Quantity = quantity,
                UnitPrice = unitPrice
                };

            await _orderService.CreateOrderAsync (order);

            System.Console.WriteLine ("Order created.");
            }
        }
    }