using System;
using Npgsql;
using System.Threading.Tasks;
using Supabase;
using Npgsql.Internal;
using System.Collections.Generic;

/************IMPORTANT***********
    THESE ARE THE DIRECTIONS FOR ADDING THE VARIABLES
    1. In the terminal, change directory to BankingSystem and 
        install nuget package using 'dotnet add package Npgsql'
    2. Copy each of the export commands below and paste them into your terminal
    3. Run the program
***********************************************************************************

export PGPASSWORD=qbxDNHmye0YZFL8v
export PGUSER=postgres
export PGHOST=db.pldcjrdychteqvmixmff.supabase.co
export PGPORT=5432
export SUPABASE_URL="https://pldcjrdychteqvmixmff.supabase.co"
export DATABASE_URL="postgresql://postgres.pldcjrdychteqvmixmff:qbxDNHmye0YZFL8v@aws-0-us-east-1.pooler.supabase.com:5432/postgres"
export SUPABASE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoiYWxsIiwiaWF0IjoxNjM0NjYwNjYzLCJleHAiOjE5NDkyMzY2NjN9.1Z6
export SUPABASE_SERVICE_KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InBsZGNqcmR5Y2h0ZXF2bWl4bWZmIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MDQyOTA1MywiZXhwIjoyMDU2MDA1MDUzfQ.0tzaHJYtXSFrwMp3DCjdm6SejQdc_pUA3ASUm89-oqM"

or, if on windows/powershell 

$env:PGPASSWORD="qbxDNHmye0YZFL8v"
$env:PGUSER="postgres"
$env:PGHOST="db.pldcjrdychteqvmixmff.supabase.co"
$env:PGPORT="5432"
$env:SUPABASE_URL="https://pldcjrdychteqvmixmff.supabase.co"
$env:DATABASE_URL="postgresql://postgres.pldcjrdychteqvmixmff:qbxDNHmye0YZFL8v@aws-0-us-east-1.pooler.supabase.com:5432/postgres"
$env:SUPABASE_KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoiYWxsIiwiaWF0IjoxNjM0NjYwNjYzLCJleHAiOjE5NDkyMzY2NjN9.1Z6"
$env:SUPABASE_SERVICE_KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InBsZGNqcmR5Y2h0ZXF2bWl4bWZmIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MDQyOTA1MywiZXhwIjoyMDU2MDA1MDUzfQ.0tzaHJYtXSFrwMp3DCjdm6SejQdc_pUA3ASUm89-oqM"
*/

namespace BankingSystem
{

    /*
    Zachary Vogel, 801333583
    This is the main class that connects to the Supabase database and allows us to interact with the database.
    We have it creating a sample user.
    Finished on 3/9/25
    */
    class Program
    {
        static async Task Main(string[] args)
        {
            // Fetch Supabase credentials from environment variables
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL")?.Trim() ?? throw new InvalidOperationException("SUPABASE_URL environment variable is not set.");
            var key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")?.Trim() ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY environment variable is not set.");

            // PostgreSQL connection string for Supabase database
            string databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? throw new InvalidOperationException("DATABASE_URL environment variable is not set.");

            // Convert the URI to a standard connection string
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            string connectionString = $"Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";
            // Initialize Supabase Client
            var options = new SupabaseOptions { AutoConnectRealtime = true };
            var supabase = new Client(url, key, options);

            try
            {
                await supabase.InitializeAsync();
                Console.WriteLine("Connected to Supabase successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Supabase client: {ex.Message}");
                return; // Exit if Supabase fails to connect
            }

            // Connect to PostgreSQL inside Supabase
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("Connected to Supabase PostgreSQL successfully!");

                    // Test Query
                    using (var cmd = new NpgsqlCommand("SELECT NOW()", conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        Console.WriteLine($"Database time: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                return;
            }

            /*
            Martin Anuonye, 801304061
            Example code below to showcase account management functionality.
            BankAccount.CreateAccount() creates a new bank account using inputted arguments and returns a userId accessed from the database
            */
            int? userId = await BankAccount.CreateAccount(connectionString, "John", "Doe", "sample@charlotte.com", "securepassword", "123-456-7890", 1000.00f, "User");
            if (userId != null)
            {
                Console.WriteLine($"The new user's ID is: {userId}");
            }
            else
            {
                Console.WriteLine("User insertion failed.");
            }

            Admin admin1 = new Admin("John", "holland", "jh01@uncc.edu", "password", "123-456-7890", 1000.00f);
            List<User> users = await admin1.AccessUserInfo(connectionString);
            foreach (var user in users)
            {
                Console.WriteLine($"User Info: ID {user.Id}, Name {user.FirstName} {user.LastName}, Email {user.Email}, Phone {user.PhoneNumber}");
            }

            //User is prompted for email and password to login
            Console.WriteLine("Enter your email: ");
            string? emailInput = Console.ReadLine();
            if (string.IsNullOrEmpty(emailInput))
            {
                Console.WriteLine("Email cannot be empty.");
                return;
            }
            string email = emailInput;


            Console.WriteLine("Enter your password: ");
            string? passInput = Console.ReadLine();
            if (string.IsNullOrEmpty(passInput))
            {
                Console.WriteLine("Password cannot be empty.");
                return;
            }
            string password = passInput;

            //UserSession.Login returns true if login was successful
            bool loginSuccess = await UserSession.Login(connectionString, email, password);
            if (loginSuccess)
            {
                if (UserSession.CurrentUser != null)
                {
                    Console.WriteLine($"Login successful! Welcome, {UserSession.CurrentUser.FirstName}.");
                }
                else
                {
                    Console.WriteLine("Login unsuccessful");
                }
            }
            else
            {
                Console.WriteLine("Login failed. Check credentials.");
                return;
            }

            //Once logged in, the user can access their account information and delete their account.
            await BankAccount.GetAccount(connectionString);
            await BankAccount.DeleteAccount(connectionString);

            //UserSession.Logout() logs out of the current user account
            UserSession.Logout();

        }

        /*
        Zachary Vogel 801333583
        This method prints all users in the database with their information.
        it takes the connection string as a parameter which allows it to open a connection to the database.
        */
        public static async Task printAllUsers(string connectionString)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"SELECT * FROM BankUsers";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Console.WriteLine($"User Info: ID {reader.GetInt32(reader.GetOrdinal("Id"))}, Name {reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}, Email {reader.GetString(reader.GetOrdinal("Email"))}, Phone {reader.GetString(reader.GetOrdinal("PhoneNumber"))}");
                            }
                        }
                    }
                    Console.WriteLine("Printing all users complete");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to print all users: {ex.Message}");
            }
        }
    }


    /*
    Zachary Vogel, 801333583
    This is the User class that is for now only a parent class for the Admin class.
    It contains all the information for a user and has methods to insert, update, delete, and get a user from the database.
    each user has a unique ID that is generated when the user is inserted into the database.
    Finished on 3/9/25
    */
    class User
    {
        public int Id { get; internal set; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
        public string Password { get; }
        public string PhoneNumber { get; }
        public float Balance { get; set; }
        public string AuthorityLevel { get; }


        /*
        Zachary Vogel, 801333583
        This is the constructor for the User class. It takes in all the information for a user and sets the properties.
        It also sets the AuthorityLevel to "User" by default.
        @param id The unique ID of the user
        @param firstName The first name of the user
        @param lastName The last name of the user
        @param email The email of the user
        @param password The password of the user
        @param phoneNumber The phone number of the user
        @param balance The balance of the user
        @param authorityLevel The authority level of the user
        */
        public User(string firstName, string lastName, string email, string password, string phoneNumber, float balance, string authorityLevel = "User")
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Password = password;
            PhoneNumber = phoneNumber;
            Balance = balance;
            AuthorityLevel = authorityLevel;
        }

        /*
        Zachary Vogel, 801333583
        Inserts a new user into the database and returns the ID of the new user.
        @param connectionString The connection string to the database
        @return The ID of the new user
        */
        public async Task<int?> InsertUserAsync(string connectionString)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"INSERT INTO BankUsers (FirstName, LastName, Email, Password, PhoneNumber, Balance, AuthorityLevel) 
                            VALUES (@FirstName, @LastName, @Email, @Password, @PhoneNumber, @Balance, @AuthorityLevel)
                            RETURNING Id;"; // Returning the newly created user ID

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("FirstName", FirstName);
                        cmd.Parameters.AddWithValue("LastName", LastName);
                        cmd.Parameters.AddWithValue("Email", Email);
                        cmd.Parameters.AddWithValue("Password", Password);
                        cmd.Parameters.AddWithValue("PhoneNumber", PhoneNumber);
                        cmd.Parameters.AddWithValue("Balance", Balance);
                        cmd.Parameters.AddWithValue("AuthorityLevel", AuthorityLevel);

                        var insertedId = await cmd.ExecuteScalarAsync();
                        Console.WriteLine($"User inserted successfully with ID: {insertedId}");

                        if (insertedId != null) Id = (int)insertedId; // Set the ID property
                        return Id; // Return the generated ID
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to insert user: {ex.Message}");
                return null; // Return null if insertion fails
            }
        }

        /*
        Zachary Vogel, 801333583
        Gets a user from the database by ID.
        @param connectionString The connection string to the database
        @param userId The ID of the user to get
        @return The user with the given ID, or null if not found
        */
        public static async Task<User?> GetUserAsync(string connectionString, int userId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"SELECT * FROM BankUsers WHERE Id = @userId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("userId", userId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User(
                                    reader.GetString(reader.GetOrdinal("FirstName")),
                                    reader.GetString(reader.GetOrdinal("LastName")),
                                    reader.GetString(reader.GetOrdinal("Email")),
                                    reader.GetString(reader.GetOrdinal("Password")),
                                    reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    reader.GetFloat(reader.GetOrdinal("Balance")),
                                    reader.GetString(reader.GetOrdinal("AuthorityLevel"))
                                )
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get user: {ex.Message}");
            }

            return null;
        }

        /*
        Zachary Vogel, 801333583
        Updates a user in the database.
        @param connectionString The connection string to the database
        @param newFirstName The new first name of the user
        @param newLastName The new last name of the user
        @param newEmail The new email of the user
        @param newPassword The new password of the user
        @param newPhoneNumber The new phone number of the user
        @param newBalance The new balance of the user
        @param newAuthorityLevel The new authority level of the user
        @return The user with the given ID, or null if not found
        */
        public async Task UpdateUserAsync(string connectionString, string newFirstName, string newLastName, string newEmail, string newPassword, string newPhoneNumber, float newBalance, string newAuthorityLevel)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Check if the new email already exists
                    var checkEmailSql = @"SELECT COUNT(*) FROM BankUsers WHERE Email = @NewEmail AND Id != @Id";
                    using (var checkEmailCmd = new NpgsqlCommand(checkEmailSql, conn))
                    {
                        checkEmailCmd.Parameters.AddWithValue("NewEmail", newEmail);
                        checkEmailCmd.Parameters.AddWithValue("Id", Id);

                        var emailCount = (long)await checkEmailCmd.ExecuteScalarAsync();
                        if (emailCount > 0)
                        {
                            Console.WriteLine("Failed to update user: Email already exists.");
                            return;
                        }
                    }

                    var sql = @"UPDATE BankUsers SET FirstName = @FirstName, LastName = @LastName, Email = @NewEmail, Password = @Password, PhoneNumber = @PhoneNumber, Balance = @Balance, AuthorityLevel = @AuthorityLevel 
                            WHERE Id = @Id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("FirstName", newFirstName);
                        cmd.Parameters.AddWithValue("LastName", newLastName);
                        cmd.Parameters.AddWithValue("NewEmail", newEmail);
                        cmd.Parameters.AddWithValue("Password", newPassword);
                        cmd.Parameters.AddWithValue("PhoneNumber", newPhoneNumber);
                        cmd.Parameters.AddWithValue("Balance", newBalance);
                        cmd.Parameters.AddWithValue("AuthorityLevel", newAuthorityLevel);
                        cmd.Parameters.AddWithValue("Id", Id);

                        await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine("User updated successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update user: {ex.Message}");
            }
        }

        /*
        Zachary Vogel, 801333583
        Deletes a user from the database. ----> it is used as Userexample.DeleteUserAsync(connectionString);
        @param connectionString The connection string to the database
        @return The user with the given ID, or null if not found
        */
        public async Task DeleteUserAsync(string connectionString)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"DELETE FROM BankUsers WHERE Id = @Id";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("Id", Id);

                        await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine("User deleted successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete user: {ex.Message}");
            }
        }
    }


    /*
    Zachary Vogel, 801333583
    This is the Admin class that inherits from the User class. It contains all the information for an admin and has methods to access user information.
    Finished on 3/9/25
    */
    class Admin : User
    {
        /*
        Zachary Vogel, 801333583
        This is the constructor for the Admin class. It takes in all the information for an admin and sets the properties.
        It also sets the AuthorityLevel to "Admin" by default.
        */
        public Admin(string firstName, string lastName, string email, string password, string phoneNumber, float balance)
            : base(firstName, lastName, email, password, phoneNumber, balance, "Admin")
        {
        }


        /*
        Zachary Vogel, 801333583
        This method accesses all the user information from the database and returns a list of users.
        @param connectionString The connection string to the database
        @return A list of users
        */
        public async Task<List<User>> AccessUserInfo(string connectionString)
        {
            List<User> users = new List<User>((int)await GetTableSize(connectionString, "bankusers"));
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"SELECT * FROM BankUsers";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                users.Add(new User(
                                    reader.GetString(reader.GetOrdinal("FirstName")),
                                    reader.GetString(reader.GetOrdinal("LastName")),
                                    reader.GetString(reader.GetOrdinal("Email")),
                                    reader.GetString(reader.GetOrdinal("Password")),
                                    reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    reader.GetFloat(reader.GetOrdinal("Balance")),
                                    reader.GetString(reader.GetOrdinal("AuthorityLevel"))
                                )
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to access user info: {ex.Message}");
            }

            return users;
        }

        /*
        Zachary Vogel, 801333583
        This method gets the size of the table in bytes. It is used in the AccessUserInfo method to create the list of users.
        @param connectionString The connection string to the database
        @param tableName The name of the table to get the size of
        @return The size of the table in bytes
        */
        public async Task<long> GetTableSize(string connectionString, string tableName)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"SELECT pg_total_relation_size(@tableName)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("tableName", tableName);

                        var size = (long)await cmd.ExecuteScalarAsync();
                        Console.WriteLine($"Table {tableName} size: {size} bytes");

                        return size;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get table size: {ex.Message}");
                return -1;
            }
        }
    }

    /*
    Martin Anuonye, 801304061
    The UserSession and BankAccount classes handle account management for our banking system. This includes account 
    creation dependent on the initial deposit required to open each type of account. This code also handles the
    logging in/out of user bank accounts to allow users to interact with their account. The code
    connects to our Supabase database and retrieves and updates data accordingly
    */
    class UserSession
    {
        //The CurrentUser instance will be used to keep track of the account currently logged into
        public static User? CurrentUser { get; private set; }

        /*
        This method uses the connection to the database to check an inputted email and password 
        against database entries to attempt to log into a matching account
        */
        public static async Task<bool> Login(string connectionString, string email, string password)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var sql = "SELECT * FROM BankUsers WHERE Email = @Email AND Password = @Password";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("Email", email);
                        cmd.Parameters.AddWithValue("Password", password);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var authorityLevel = reader.GetString(reader.GetOrdinal("AuthorityLevel"));
                                //CurrentUser's type is dependent on the authority level of the requested user
                                if (authorityLevel == "Admin")
                                {
                                    CurrentUser = new Admin(
                                        reader.GetString(reader.GetOrdinal("FirstName")),
                                        reader.GetString(reader.GetOrdinal("LastName")),
                                        reader.GetString(reader.GetOrdinal("Email")),
                                        reader.GetString(reader.GetOrdinal("Password")),
                                        reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                        reader.GetFloat(reader.GetOrdinal("Balance"))
                                    )
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id"))
                                    };
                                }
                                else
                                {
                                    CurrentUser = new User(
                                        reader.GetString(reader.GetOrdinal("FirstName")),
                                        reader.GetString(reader.GetOrdinal("LastName")),
                                        reader.GetString(reader.GetOrdinal("Email")),
                                        reader.GetString(reader.GetOrdinal("Password")),
                                        reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                        reader.GetFloat(reader.GetOrdinal("Balance")),
                                        authorityLevel
                                    )
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id"))
                                    };


                                    Console.WriteLine("Login successful!");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
            }

            Console.WriteLine("Invalid email or password.");
            return false;
        }

        //Logs out of user account currently being accessed
        public static void Logout()
        {
            CurrentUser = null;
            Console.WriteLine("User logged out.");
        }
    }

    class BankAccount
    {
        /*
        This method creates a new user account and adds it to the database using InsertUserAsync(). It checks for account 
        type and creates the specific account accordingly
        */
        public static async Task<int?> CreateAccount(string connectionString, string firstName, string lastName, string email, string password, string phoneNumber, float initialDeposit, string AuthorityLevel)
        {
            float minDeposit = AuthorityLevel == "Admin" ? 5000f : AuthorityLevel == "Family" ? 100f : 0f;
            if (initialDeposit < minDeposit)
            {
                Console.WriteLine($"Minimum deposit for {AuthorityLevel} account is ${minDeposit}.");
                return null;
            }

            var user = AuthorityLevel == "Admin"
            ? new Admin(firstName, lastName, email, password, phoneNumber, initialDeposit)
            : new User(firstName, lastName, email, password, phoneNumber, initialDeposit, AuthorityLevel);

            int? userId = await user.InsertUserAsync(connectionString);

            if (AuthorityLevel == "Family" && userId != null)
            {
                await CreateChildAccount(connectionString, userId.Value);
            }

            return userId;
        }

        //Method to handle child account creation
        private static async Task CreateChildAccount(string connectionString, int parentId)
        {
            var child = new User("Child", "Account", $"child{parentId}@bank.com", "password", "000-000-0000", 100f, "Child");
            await child.InsertUserAsync(connectionString);
            Console.WriteLine("Child account created with $100 balance.");

        }
        
        //This methods accesses the CurrentUser to print its account information and return the user's Id using GetUserAsync()
        public static async Task<int?> GetAccount(string connectionString)
        {
            if (UserSession.CurrentUser == null)
            {
                Console.WriteLine("No user is currently logged in.");
                return null;
            }


            var userId = UserSession.CurrentUser.Id;
            var user = await User.GetUserAsync(connectionString, userId);
            if (user != null)
            {
                Console.WriteLine($"User Info: ID {user.Id}, Name {user.FirstName} {user.LastName}, Email {user.Email}, Phone {user.PhoneNumber}, Balance {user.Balance}, Authority Level {user.AuthorityLevel}");
                return user.Id;
            }
            else
            {
                Console.WriteLine("Failed to retrieve user.");
                return null;
            }
        }

        //This method deletes the CurrentUser's account and information from the database using DeleteUserAsync()
        public static async Task<bool?> DeleteAccount(string connectionString)
        {
            if (UserSession.CurrentUser == null)
            {
                Console.WriteLine("No user is currently logged in.");
                return null;
            }

            try
            {
                await UserSession.CurrentUser.DeleteUserAsync(connectionString);
                Console.WriteLine("Account deleted successfully.");
                UserSession.Logout();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete account: {ex.Message}");
                return false;
            }
        }

    }

    /*
    James Pulliam, 801389134
    I created the MoneyFunctionality class whose job it is to interact with the money in the account. 
    With it you can deposit and withdraw money alongside taking out and repaying loans.
    */
    class MoneyFunctionality
    {
        public float LoanAmount { get; set; }

        // Method used to deposit money into your account
        public void Deposit(User user, float amountDeposited)
        {
            if (amountDeposited > 0)
            {
                user.Balance += amountDeposited; // Update the user's balance directly
                Console.WriteLine($"Deposit successful! New balance: {user.Balance:C}");
            }
            else
            {
                Console.WriteLine("Deposit amount must be positive.");
            }
        }

        // Method used to withdraw money from your account
        public void Withdraw(User user, float amount)
        {
            if (amount > 0 && amount <= user.Balance)
            {
                user.Balance -= amount; // Update the user's balance directly
                Console.WriteLine($"Withdrawal successful! New balance: {user.Balance:C}");
            }
            else
            {
                Console.WriteLine("Insufficient balance or invalid amount.");
            }
        }

        // Method used in order to take a loan where you would need to type in the amount you want and then the interestRate. 
        // If you take multiple loans, they add together.
        public void TakeLoan(User user, float requestedAmount, float interestRate)
        {
            // Interest rate must be positive and requestedAmount must be a positive amount greater than zero.
            if (requestedAmount > 0 && interestRate > 0)
            {
                float interest = requestedAmount * (interestRate / 100);
                float totalLoan = requestedAmount + interest;
                LoanAmount += totalLoan; // Adds to the loan amount

                user.Balance += requestedAmount; // Add the requested loan amount to the user's balance

                Console.WriteLine($"Loan approved! You received {requestedAmount:C} with an interest rate of {interestRate}%.");
                Console.WriteLine($"Total loan payable: {LoanAmount:C}. New balance: {user.Balance:C}");
            }
            else
            {
                Console.WriteLine("Invalid loan amount or interest rate.");
            }
        }

        // Method used in order to repay a loan 
        public void RepayLoan(User user, float amount)
        {
            // Ensures you cannot use this method if you do not have a current loan
            if (LoanAmount == 0)
            {
                Console.WriteLine("No outstanding loan to repay.");
                return;
            }

            if (amount > 0 && amount <= user.Balance)
            {
                // Happens if you fully pay the loan
                if (amount >= LoanAmount)
                {
                    user.Balance -= LoanAmount; // Subtract the full loan amount from the user's balance
                    Console.WriteLine($"Loan fully repaid! Remaining balance: {user.Balance:C}");
                    LoanAmount = 0; // Reset the loan amount after it's fully paid
                }
                // Happens if you pay part of the loan
                else
                {
                    user.Balance -= amount; // Subtract the repayment amount from the user's balance
                    LoanAmount -= amount; // Reduce the loan amount by the repayment amount
                    Console.WriteLine($"Partial payment of {amount:C} made. Remaining loan balance: {LoanAmount:C}");
                    Console.WriteLine($"New balance: {user.Balance:C}");
                }
            }
            // Happens if you do not have enough in your account to pay the loan or try to pay negative money
            else
            {
                Console.WriteLine("Insufficient balance or invalid amount.");
            }
        }
    }




}
