/*
    * The Services.cs file contains the UserService class, which is responsible for managing user-related operations.
    * It interacts with the Supabase client to perform actions such as loading user data, signing up users, and managing user sessions.
    * The SupabaseService class is responsible for initializing the Supabase client and managing the PostgreSQL database connection.
    * It provides methods for signing up users, restoring sessions, and finding users by email.
    * We use the UserService class to manage user-related operations in the application by injecting the class into the Blazor components.
*/


namespace BlazorBankingApp.Services;

using Npgsql;
using Supabase;
using Supabase.Gotrue;
using Supabase.Postgrest;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Runtime.CompilerServices;


/// <summary>
/// UserService class is responsible for managing user-related operations.
/// It interacts with the Supabase client to perform actions such as loading user data, signing up users, and managing user sessions.
/// </summary>
public class UserService
{
    //Supabase client is the current instance of our supabase project.
    private readonly Supabase.Client _supabaseClient;

    //SupabaseService is an instance of the SupabaseService class, which is responsible for initializing the Supabase client and managing the PostgreSQL database connection.
    private SupabaseService _supabaseService;

    //CurrentUser is the current user of the application (BankUser).
    public BankUser CurrentUser;

    //User properties
    public string? id { get; set; }
    public string? email { get; set; }
    public string? authoritylevel { get; set; }
    public string? name { get; set; }
    public string? phone { get; set; }
    public float balance { get; set; }

    /// <summary>
    /// Initializes the UserService instance with the given Supabase client.
    /// </summary>
    /// <param name="supabaseClient">The Supabase client instance.</param>
    public UserService(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }


    /// <summary>
    /// Loads the current bank user's data and updates the UserService properties.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the loaded 
    /// <see cref="BankUser"/> object if successful; otherwise, null if the user data could not be loaded.
    /// </returns>
    public async Task<BankUser?> LoadBankUser()
    {
        //Loading the user data
        var bankUser = await GetBankUserData();
        if (bankUser == null)
        {
            Console.WriteLine("Failed to load user data. - LoadBankUser.Services.cs");
            return null;
        }

        //setting the user properties
        id = bankUser.Id;
        balance = (float)bankUser.Balance;
        authoritylevel = bankUser.AuthorityLevel;
        name = bankUser.Name;
        phone = bankUser.Phone;

        CurrentUser = bankUser;
        Console.WriteLine($"Loaded user: {email}, Balance: {balance}");
        return bankUser;
    }


    /// <summary>
    /// Loads the current user's data from both the Supabase Auth and the BankUser table.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a tuple of the loaded
    /// <see cref="Supabase.Gotrue.User"/> and the corresponding <see cref="BankUser"/> object if successful; otherwise, null if the user data could not be loaded.
    /// </returns>
    private async Task<(Supabase.Gotrue.User?, BankUser?)> GetFullUserData()
    {
        // Restore session to ensure the user is authenticated
        _supabaseService.RestoreSession();
        var session = _supabaseClient.Auth.CurrentSession;
        if (session == null)
        {
            Console.WriteLine("No active session found. - GetFullUserData.Services.cs");
            return (null, null);
        }

        // Get the user from Supabase Auth
        // and the corresponding BankUser from the database
        try
        {
            var user = await _supabaseClient.Auth.GetUser(session.AccessToken).ConfigureAwait(false);
            if (user == null) return (null, null);

            // Get the BankUser data from the database
            var bankUser = await _supabaseClient
                .From<BankUser>()
                .Select("*")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id)
                .Single();

            return (user, bankUser);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user: {ex.Message} - {ex.StackTrace ?? "GetFullUserData.Services.cs"}");
            return (null, null);
        }
    }

    /// <summary>
    /// Retrieves the current bank user's data by loading it from the database.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the loaded
    /// <see cref="BankUser"/> object if successful; otherwise, null if the user data could not be loaded.
    /// </returns>
    public async Task<BankUser?> GetBankUserData()
    {
        BankUser bankUser = await LoadBankUser();
        Console.WriteLine($"Loaded user: {_supabaseClient.Auth.CurrentSession?.User?.Email}Balance: {bankUser?.Balance} - GetBankUserData.Services.cs");
        return bankUser;
    }

    /// <summary>
    /// Retrieves the current user's balance from the BankUser table in the PostgreSQL database.
    /// </summary>
    /// <remarks>
    /// This method is used to load the user's balance when the application starts
    /// and to update the balance when a transaction is made.
    /// <para>
    /// The method first checks if there is an active session. If not, it returns without doing anything.
    /// Then, it retrieves the user's balance from the BankUser table by filtering on the user's ID.
    /// If the balance is found, it is stored in the <see cref="balance"/> property.
    /// If the balance cannot be found, a message is written to the console.
    /// </para>
    /// </remarks>
    public async Task SetBalance()
    {

        using var supabaseService = new SupabaseService();
        await supabaseService.RestoreSession();
        if (supabaseService.GetClient().Auth.CurrentSession == null)
        {
            Console.WriteLine("No active session found. - SetBalance.Services.cs");
            return;
        }

        // Get the current user's balance from the BankUser table
        BankUser bankUser = await supabaseService.GetClient()
                .From<BankUser>()
                .Select("balance")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, supabaseService.GetClient().Auth.CurrentSession?.User?.Id ?? throw new InvalidOperationException("User is not authenticated."))
                .Single();

        if (bankUser != null)
        {
            balance = (float)bankUser.Balance;
        }
        else
        {
            Console.WriteLine("Failed to load user balance. - SetBalance.Services.cs");
        }
    }
}

/// <summary>
/// SupabaseService class is responsible for initializing the Supabase client and managing the PostgreSQL database connection.
/// It provides methods for signing up users, restoring sessions, and finding users by email.
/// </summary>
public class SupabaseService : IDisposable
{
    //Supabase client is the current instance of our supabase project.
    private readonly Supabase.Client _supabaseClient;

    //PostgreSQL Database Connection
    private readonly NpgsqlConnection _dbConnection;


    /// <summary>
    /// Initializes a new instance of the <see cref="SupabaseService"/> class.
    /// This constructor sets up the Supabase client and PostgreSQL database connection using environment variables.
    /// It first retrieves the necessary environment variables for Supabase and the database. 
    /// Then, it initializes the Supabase client and attempts to restore the session asynchronously at startup.
    /// Additionally, it establishes a connection to the PostgreSQL database, opening it for use in the service.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any of the required environment variables ("SUPABASE_URL", "SUPABASE_SERVICE_KEY", "DATABASE_URL") are not set.
    /// </exception>
    public SupabaseService()
    {

        //Sets up the Supabase client using environment variables
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL")?.Trim()
            ?? throw new InvalidOperationException("SUPABASE_URL environment variable is not set. - SupabaseService.cs");
        var key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")?.Trim()
            ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY environment variable is not set. - SupabaseService.cs");
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ??
            throw new InvalidOperationException("DATABASE_URL environment variable is not set. - SupabaseService.cs");

        // Initialize Supabase client
        var options = new SupabaseOptions { AutoConnectRealtime = true };
        _supabaseClient = new Supabase.Client(url, key, options);

        // Restore session at startup
        Task.Run(async () =>
        {
            await _supabaseClient.InitializeAsync();
            await RestoreSession();
        }).Wait();

        Console.WriteLine(" Supabase Client initialized. - SupabaseService.cs");

        //PostgreSQL Database Connection Setup
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        string connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";

        // Initialize and open PostgreSQL connection
        _dbConnection = new NpgsqlConnection(connectionString);
        _dbConnection.Open();
        Console.WriteLine(" PostgreSQL connection opened.");
    }

    /// <summary>
    /// Gets the Supabase client instance.
    /// </summary>
    /// <returns> 
    /// The Supabase client instance.
    /// </returns>
    public Supabase.Client GetClient() => _supabaseClient;

    /// <summary>
    /// Gets the PostgreSQL database connection instance.
    /// </summary>
    /// <returns>
    /// The PostgreSQL database connection instance.
    /// </returns>
    public NpgsqlConnection GetConnection() => _dbConnection;



    /// <summary>
    /// Signs up a new user using the Supabase client's Auth.SignUp method, and then inserts a new BankUser into the database with the user's email, name, phone number, authority level, and balance.
    /// </summary>
    /// <param name="email">The email address of the user to sign up.</param>
    /// <param name="password">The password of the user to sign up.</param>
    /// <param name="displayName">The display name of the user to sign up.</param>
    /// <param name="phone">The phone number of the user to sign up.</param>
    /// <param name="authorityLevel">The authority level of the user to sign up, which can be either "user" or "admin".</param>
    /// <returns>The signed up user, or null if the signup failed.</returns>
    public async Task<Supabase.Gotrue.User?> SignUpUser(string email, string password, string displayName, string phone, string authorityLevel)
    {
        try
        {
            // Sign up user into Supabase Auth
            var authResponse = await _supabaseClient.Auth.SignUp(email.Trim(), password);

            // Check if the signup was successful
            if (authResponse.User == null)
            {
                Console.WriteLine("Signup failed.");
                return null;
            }

            if (authResponse.User != null)
            {
                // No session means we should log in manually
                var loginResponse = await _supabaseClient.Auth.SignIn(email.Trim(), password);
                if (loginResponse.User == null)
                {
                    Console.WriteLine("Login failed after signup.");
                    return null;
                }
                else
                {
                    // Set the session manually
                    Console.WriteLine("Login successful after signup.");
                }
            }

            // Save session immediately
            await SaveSession();

            await Task.Delay(500);

            // Get the user ID and email from the auth response
            var userId = authResponse.User.Id ?? throw new InvalidOperationException("User ID cannot be null");
            var userEmail = authResponse.User.Email ?? email; // <--- use auth user email, or fallback to original

            try
            {
                // Check if BankUser exists already (we do this because when a new auth.user is created, a new bankUser is automatically created but we need to update it)
                // Get the existing BankUser from the database
                var existingUserResp = await _supabaseClient
                    .From<BankUser>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Get();

                var existingUser = existingUserResp.Models.FirstOrDefault();

                //Updates the existing BankUser if it exists
                if (existingUser != null)
                {
                    // Update existing BankUser
                    existingUser.Name = displayName;
                    existingUser.Phone = phone;
                    existingUser.AuthorityLevel = authorityLevel;
                    existingUser.Balance = 20000;
                    existingUser.Email = userEmail; // <-- use userEmail now

                    await _supabaseClient
                        .From<BankUser>()
                        .Update(existingUser);

                    Console.WriteLine("User updated successfully in the database - SignUpUser.Services.cs");
                }
                else
                {
                    // Inserts new BankUser if it doesn't exist
                    var newUser = new BankUser
                    {
                        Id = userId,
                        Name = displayName,
                        Phone = phone,
                        AuthorityLevel = authorityLevel,
                        Balance = 20000, // <-- default balance
                        Email = userEmail // <-- use userEmail now
                    };

                    // Insert new BankUser into the database
                    await _supabaseClient
                        .From<BankUser>()
                        .Insert(newUser);

                    Console.WriteLine("BankUser inserted successfully into the database - SignUpUser.Services.cs");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating BankUser: {ex.Message} - {ex.StackTrace ?? "SignUpUser.Services.cs"}");
            }

            Console.WriteLine($"User signed up successfully: {authResponse.User.Email} - {authResponse.User.Id ?? "SignUpUser.Services.cs"}");
            return authResponse.User;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error signing up: {ex.Message} - {ex.StackTrace ?? "SignUpUser.Services.cs"}");
            return null;
        }
    }



    /// <summary>
    /// Saves the current session to a file named "session.json".
    /// If the session is null, nothing is saved.
    /// </summary>
    public async Task SaveSession()
    {
        var session = _supabaseClient.Auth.CurrentSession;
        if (session != null)
        {
            var sessionJson = JsonSerializer.Serialize(session);
            await File.WriteAllTextAsync("session.json", sessionJson);
            Console.WriteLine("Session saved. - SaveSession.Services.cs");
        }
        else
        {
            Console.WriteLine("No session to save. - SaveSession.Services.cs");
        }
    }

    /// <summary>
    /// Restores the session from a file named "session.json".
    /// If the file does not exist, it will not throw an error.
    /// If the session is restored successfully, it will be set in the Supabase client.
    /// </summary>
    public async Task RestoreSession()
    {
        try
        {
            // Check if the session file exists
            if (File.Exists("session.json"))
            {
                var sessionJson = await File.ReadAllTextAsync("session.json");
                var session = JsonSerializer.Deserialize<Session>(sessionJson);

                if (session != null)
                {
                    await _supabaseClient.Auth.SetSession(session.AccessToken, session.RefreshToken);
                    Console.WriteLine("Session restored. - RestoreSession.Services.cs");
                }
            }
            else
            {
                Console.WriteLine(" No saved session found. - RestoreSession.Services.cs");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Failed to restore session: {ex.Message} - {ex.StackTrace ?? "RestoreSession.Services.cs"}");
        }
    }

    /// <summary>
    /// Finds a user by email in the BankUser table.
    /// </summary>
    /// <param name="email">The email address of the user to find.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the found
    /// <see cref="BankUser"/> object if successful; otherwise, null if the user was not found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user is not authenticated.
    /// </exception>
    public async Task<BankUser> FindUserByEmailAsync(string email)
    {
        var user = await _supabaseClient
            .From<BankUser>()
            .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
            .Single();

        return user;
    }


    /// <summary>
    /// Disposes of the Supabase client and PostgreSQL database connection.
    /// </summary>
    public void Dispose()
    {
        _dbConnection?.Close();
        _dbConnection?.Dispose();
        Console.WriteLine(" PostgreSQL connection closed. - SupabaseService.cs");
        Console.WriteLine(" Supabase client cleanup completed. - SupabaseService.cs");
    }
}
