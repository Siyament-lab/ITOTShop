using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Enums;
using ITOTShop.OrderConsole.Interfaces;
using ITOTShop.OrderConsole.Services;
using Microsoft.AspNetCore.Identity;

namespace ITOTShop.Tests.Console
    {
    public class AuthServicetests
        {
        //****Test 1**** Tom användarnamn
        [Fact]
        public async Task Test1_RegisterAsync_EmptyUsername_IsRejected ()
            {
            //Arrange: arrangerar en falsk dataaccess så testet inte behöver databas
            var userDataAccess = new FakeUserDataAccess ();
            //Samma passwordHash som den riktiga applikationen
            var passwordHasher = new PasswordHasher<User> ();
            var authService = new AuthService (
                userDataAccess,
                passwordHasher);

            //Act: Försöker registera en användare för kund(Customer) utan användarnamn
            async Task Act ()
                {
                await authService.RegisterAsync (
                    "", //Användarnamn tom
                    "TestPassword123!",
                    UserRole.Customer);
                }

            //Assert :kontrollerar att rätt valideringsfel returneras
            //Här ska uppmaning om "användarnamn efterfrågas" skrivas.
            await Assert.ThrowsAsync<ArgumentException> (Act);

            }

        //***Test 2**** Giltigt användarnamn
        [Fact]
        public async Task Test2_RegisterAsync_ValidUser_CreatesUser ()
            {
            // Arrange
            var userDataAccess = new FakeUserDataAccess ();
            var passwordHasher = new PasswordHasher<User> ();

            var authService = new AuthService (
                userDataAccess,
                passwordHasher);


            // Act
            async Task Act ()
                {
                await authService.RegisterAsync (
                    "testuser",
                    "TestPassword123!",
                    UserRole.Customer);
                }

            await Act ();


            // Assert
            // Hämtar användaren från vår FakeUserDataAccess.
            var createdUser =
                await userDataAccess.GetByUsernameAsync ("testuser");

            // Kontrollerar att användaren faktiskt skapades.
            Assert.NotNull (createdUser);

            // Kontrollerar att rätt användarnamn sparades.
            Assert.Equal (
                "testuser",
                createdUser.Username);

            // Kontrollerar att användaren fick rätt roll.
            Assert.Equal (
                UserRole.Customer,
                createdUser.Role);
            }
        //*****Test 3**** Användarnamn existerar
        [Fact]
        public async Task Test3_RegisterAsync_ExistingUsername_IsRejected ()
            {
            // Arrange
            var userDataAccess = new FakeUserDataAccess ();
            var passwordHasher = new PasswordHasher<User> ();

            var authService = new AuthService (
                userDataAccess,
                passwordHasher);


            // Skapar första användaren.
            await authService.RegisterAsync (
                "testuser",
                "TestPassword123!",
                UserRole.Customer);


            // Act
            // Försöker registrera samma användarnamn igen.
            async Task Act ()
                {
                await authService.RegisterAsync (
                    "testuser",
                    "AnotherPassword123!",
                    UserRole.Customer);
                }


            // Assert
            // Kontrollerar att duplicerat användarnamn nekas.
            await Assert.ThrowsAsync<ArgumentException> (Act);
            }
        // ****Test 4**** korrekt login (användarnamn & lösen)
        [Fact]
        public async Task Test4_LoginAsync_CorrectCredentials_ReturnsUser()
            {
            //Arrange
            var userDataAccess = new FakeUserDataAccess ();
            var passwordHasher = new PasswordHasher<User> ();

            var authService = new AuthService (
                userDataAccess,
                passwordHasher);

            //1. skapar användare
            await authService.RegisterAsync (
                "testuser",
                "TestPassword123!",
                UserRole.Customer);

            //Act
            //2. loggar in med korrekt anv & lösen
            var loggedInUser = await authService.LoginAsync (
                "testuser",
                "TestPassword123!");

            //Assert
            //Kontrollerar att använadre returneras
            Assert.Equal (
                "testuser",
                loggedInUser.Username);

            //Kontroll av användarroll
            Assert.Equal (
                UserRole.Customer,
                loggedInUser.Role);


            }
        //**** Test 5**** Lösen inte sparats i klartext
        [Fact]
        public async Task Test5_RegisterAsync_Password_IsHashed ()
            {
            // Arrange
            var userDataAccess = new FakeUserDataAccess ();
            var passwordHasher = new PasswordHasher<User> ();

            var authService = new AuthService (
                userDataAccess,
                passwordHasher);

            //Klartextlösen som används för att kontrollera att PasswordHash inte sparas i klartext.
            string password = "TestPassword123!";


            // Act
            await authService.RegisterAsync (
                "testuser",
                password,
                UserRole.Customer);

            var createdUser =
                await userDataAccess.GetByUsernameAsync ("testuser");


            // Assert
            // Kontrollerar först att användaren skapades.
            Assert.NotNull (createdUser);

            // Kontrollerar att lösenordet inte har sparats i klartext.
            Assert.NotEqual (
                password,
                createdUser.PasswordHash);

            // Kontrollerar att hashen faktiskt motsvarar rätt lösenord.
            var verificationResult =
                passwordHasher.VerifyHashedPassword (
                    createdUser,
                    createdUser.PasswordHash,
                    password);

            Assert.NotEqual (
                PasswordVerificationResult.Failed,
                verificationResult);
            }

        //**** Test 6 **** Fel lösen ska inte logga in
        [Fact]
        public async Task Test6_LoginAsync_WrongPassword_ReturnsNull ()
            {
            // Arrange
            var userDataAccess = new FakeUserDataAccess ();
            var passwordHasher = new PasswordHasher<User> ();

            var authService = new AuthService (
                userDataAccess,
                passwordHasher);

            // Skapar först en användare med korrekt lösenord.
            await authService.RegisterAsync (
                "testuser",
                "TestPassword123!",
                UserRole.Customer);


            // Act
            // Försöker logga in med fel lösenord.
            var loggedInUser = await authService.LoginAsync (
                "testuser",
                "WrongPassword123!");


            // Assert
            // LoginAsync ska returnera null när lösenordet är fel.
            Assert.Null (loggedInUser);
            }
        //==========Klass för att skapa en fake dataaccess för testen ===============
        public class FakeUserDataAccess: IUserDataAccess
            {
            private readonly List<User> _users = new ();
            public Task<User?> GetByUsernameAsync ( string username )
                {
                var user = _users.FirstOrDefault (
                    u => u.Username == username);

                return Task.FromResult (user);
                }


            public Task AddAsync ( User user )
                {
                _users.Add (user);

                return Task.CompletedTask;
                }


            public Task<bool> UsernameExistsAsync ( string username )
                {
                bool exists = _users.Any (
                    u => u.Username == username);

                return Task.FromResult (exists);
                }


            public Task<List<User>> GetAllAsync ()
                {
                return Task.FromResult (
                    _users.ToList ());
                }


            public Task UpdateAsync ( User user )
                {
                return Task.CompletedTask;
                }
            }
        }
    }
