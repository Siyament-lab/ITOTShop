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
        public async Task RunAsync (User currentUser)
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
                        await CreateOrderAsync (currentUser);
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

            //KOntrollerar om det finns några ordrar att visa, annars skrivs ett meddelande ut
            if(orders.Count == 0)
                { Console.WriteLine ("No orders found.");
                return;
                }

            foreach (var order in orders)
                {
                Console.WriteLine (
                    $"Id: {order.Id}, " +
                    $"Product: {order.ProductName}, " +
                    $"Quantity: {order.Quantity}, " +
                    $"Unit price: {order.UnitPrice}, " +
                    $"Status: {order.Status}, " +
                    $"Created: {order.CreatedAt:yyyy-MM-dd HH:mm}");
                }
            }

        // Läser in orderdata från användaren och skapar en ny order
        private async Task CreateOrderAsync (User currentUser)
            {
            string productName;
            //Do-while loop för att säkerställa att produktnamnet inte är tomt
            do
                {
                Console.Write ("Product name: ");
                productName = Console.ReadLine () ?? string.Empty;
                if(string.IsNullOrWhiteSpace (productName))
                    {
                    Console.WriteLine ("Product name cannot be empty. Try again!");
                    }
                }
            while (string.IsNullOrWhiteSpace (productName));

            Console.Write ("Quantity: ");
           int quantity;
            // Läser in kvantitet och kontrollerar att det är ett giltigt heltal

            while (!int.TryParse(Console.ReadLine(), out quantity) || quantity <= 0)
                {
                Console.WriteLine ("Invalid value, please enter a greater than 0:");
                }
            
            

            Console.Write ("Unit price: ");
            decimal unitPrice;
            //Konverterar inmatning till decimal
            while (!decimal.TryParse(Console.ReadLine (), out unitPrice) || unitPrice <= 0)
                {
                Console.WriteLine ("Invalid value, please enter a price greater than 0:");
                
                }
           

            var order = new Order
            
                {
                // koppla ordern till den inloggade användaren.
                CustomerId = currentUser.Id,

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