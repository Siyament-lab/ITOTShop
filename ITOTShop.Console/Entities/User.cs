using System;
using System.Collections.Generic;
using System.Text;
using ITOTShop.Console.Enums;

namespace ITOTShop.Console.Entities
    {
    public class User
        {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } =string.Empty;
        public UserRole Role { get; set; }
        public bool IsActrive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Ser till att alla orders som är kopplade till en användare laddas in när användaren hämtas från databasen.
        public ICollection<Order> Orders { get; set; } = new List<Order> ();

        }
    }
