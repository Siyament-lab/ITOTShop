using ITOTShop.OrderConsole.Services;

namespace ITOTShop.OrderConsole.Menu
    {
    public class AdminMenu
        {
        private readonly OrderService _orderService;
        private readonly AuthService _authService;

        // Tar emot tjänster för order- och användarhantering via DI
        public AdminMenu (
            OrderService orderService,
            AuthService authService )
            {
            _orderService = orderService;
            _authService = authService;
            }

        // Startar huvudmenyn för administratören
        public async Task RunAsync ()
            {
            bool running = true;

            while (running)
                {
                Console.WriteLine ("\n--- Admin Menu ---");
                Console.WriteLine ("1. List all orders");
                Console.WriteLine ("2. List users");
                Console.WriteLine ("0. Exit");
                Console.Write ("Choose option: ");

                var choice = Console.ReadLine ();

                switch (choice)
                    {
                    case "1":
                        await ListOrdersAsync ();
                        break;

                    case "2":
                        await ListUsersAsync ();
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

        // Hämtar och visar alla ordrar oavsett kund
        private async Task ListOrdersAsync ()
            {
            var orders = await _orderService.GetAllOrdersAsync ();

            if (orders.Count == 0)
                {
                Console.WriteLine ("No orders found.");
                return;
                }

            foreach (var order in orders)
                {
                Console.WriteLine (
                    $"Id: {order.Id}, " +
                    $"CustomerId: {order.CustomerId}, " +
                    $"Product: {order.ProductName}, " +
                    $"Quantity: {order.Quantity}, " +
                    $"Unit price: {order.UnitPrice}, " +
                    $"Status: {order.Status}");
                }
            }

        // Hämtar och visar alla användare
        private async Task ListUsersAsync ()
            {
            var users = await _authService.GetAllUsersAsync ();

            foreach (var user in users)
                {
                Console.WriteLine (
                    $"Id: {user.Id}, " +
                    $"Username: {user.Username}, " +
                    $"Role: {user.Role}, " +
                    $"Created: {user.CreatedAt:yyyy-MM-dd HH:mm}");
                }
            }
        }
    }