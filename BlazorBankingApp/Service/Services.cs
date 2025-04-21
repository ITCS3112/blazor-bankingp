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

public class UserService
{
    private readonly Supabase.Client _supabaseClient;
    private SupabaseService _supabaseService;
    public BankUser CurrentUser;

    public string? id { get; set; }
    public string? email { get; set; }
    public string? authoritylevel { get; set; }
    public string? name { get; set; }
    public string? phone { get; set; }
    public float balance { get; set; }

    public UserService(SupabaseService supabaseService)
    {
        _supabaseClient = supabaseService.GetClient();
        
    }


    public async Task<BankUser?> LoadBankUser()
    {
        var bankUser = await GetBankUserData();
        if (bankUser == null)
        {
            Console.WriteLine("Failed to load user data.");
            return null;
        }

        id = bankUser.Id;
        balance = (float)bankUser.balance;
        authoritylevel = bankUser.AuthorityLevel;

        CurrentUser = bankUser;
        Console.WriteLine($"Loaded user: {email}, Balance: {balance}");
        return bankUser;
    }

    private async Task<(Supabase.Gotrue.User?, BankUser?)> GetFullUserData()
    {
        _supabaseService.RestoreSession();
        var session = _supabaseClient.Auth.CurrentSession;
        if (session == null)
        {
            Console.WriteLine("No active session found.");
            return (null, null);
        }

        try
        {
            var user = await _supabaseClient.Auth.GetUser(session.AccessToken).ConfigureAwait(false);
            if (user == null) return (null, null);

            var bankUser = await _supabaseClient
                .From<BankUser>()
                .Select("*")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id)
                .Single();
            
            
            return (user, bankUser);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user: {ex.Message}");
            return (null, null);
        }
    }

    public async Task<BankUser?> GetBankUserData()
    {
        BankUser bankUser = await LoadBankUser();
        Console.WriteLine($"Loaded user: {_supabaseClient.Auth.CurrentSession?.User?.Email}Balance: {bankUser?.balance}");
        return bankUser;
    }

    public async Task SetBalance()
    {
<<<<<<< HEAD:BlazorBankingApp/Service/Services.cs
        
        using var supabaseService = new SupabaseService();
        await supabaseService.RestoreSession();
        if (supabaseService.GetClient().Auth.CurrentSession == null)
        {
            Console.WriteLine("No active session found.");
            return;
        }
        BankUser bankUser = await supabaseService.GetClient()
=======
        try
        {
            var userId = _supabaseClient.Auth.CurrentSession?.User?.Id
                         ?? throw new InvalidOperationException("User is not authenticated.");

            var bankUser = await _supabaseClient
>>>>>>> eb70c34 (Logout page created, Loan page works):BlazorBankingApp/Services.cs
                .From<BankUser>()
                .Select("balance")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Single();

            balance = (float)bankUser.balance;
            Console.WriteLine($"[SetBalance] User balance loaded: {balance}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetBalance] Error: {ex.Message}");
        }
    }

    public async Task<bool> AddToBalance(float amountToAdd)
    {
        try
        {
            var userId = _supabaseClient.Auth.CurrentSession?.User?.Id;

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("User is not authenticated. - Services.AddToBalance");
                return false;
            }

            Console.WriteLine($"Supabase Auth ID: {userId} - Services.AddToBalance");

            var response = await _supabaseClient
                .From<BankUser>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Get();

            if (response.Models == null || !response.Models.Any())
            {
                Console.WriteLine("No user model found for given ID. - Services.AddToBalance");
                return false;
            }

            var user = response.Models.First();
            Console.WriteLine($"Current balance: {user.balance} - Services.AddToBalance");

            user.balance += (decimal)amountToAdd;

            var updateResponse = await _supabaseClient
                .From<BankUser>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Update(user);

            if (updateResponse != null && updateResponse.Models.Count > 0)
            {
                Console.WriteLine("Balance update successful. - Services.AddToBalance");
                balance = (float)user.balance;
                return true;
            }
            else
            {
                Console.WriteLine("Balance update failed - update response empty. - Services.AddToBalance");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating balance: {ex.Message} - Services.AddToBalance");
            return false;
        }
    }
}



public class SupabaseService : IDisposable
{
    private readonly Supabase.Client _supabaseClient;
    private readonly NpgsqlConnection _dbConnection;

    public SupabaseService()
    {
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL")?.Trim()
            ?? throw new InvalidOperationException("SUPABASE_URL environment variable is not set.");
        var key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")?.Trim()
            ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY environment variable is not set.");
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ??
            throw new InvalidOperationException("DATABASE_URL environment variable is not set.");

        var options = new SupabaseOptions { AutoConnectRealtime = true };
        _supabaseClient = new Supabase.Client(url, key, options);

        // 🔹 Restore session at startup
        Task.Run(async () =>
        {
            await _supabaseClient.InitializeAsync();
            await RestoreSession();
        }).Wait();

        Console.WriteLine(" Supabase Client initialized.");

        // 🔹 PostgreSQL Database Connection Setup
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        string connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";

        _dbConnection = new NpgsqlConnection(connectionString);
        _dbConnection.Open();
        Console.WriteLine(" PostgreSQL connection opened.");

    }

    public Supabase.Client GetClient() => _supabaseClient;
    public NpgsqlConnection GetConnection() => _dbConnection;

    public async Task<Supabase.Gotrue.User?> SignUpUser(string email, string password, string displayName, string phone)
    {
        try
        {
            Console.WriteLine($" Signing up user: {email}");
            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "name", (object?)displayName ?? ""},
                    { "phone", (object?)phone ?? ""}
                }
            };

            if (_supabaseClient == null)
            {
                throw new InvalidOperationException("Supabase client is not initialized.");
            } else {
                Console.WriteLine($" Supabase client is initialized {_supabaseClient.ToString}.");
            }
            Console.WriteLine($"Email: {email}, Password: {password}, DisplayName: {displayName}, Phone: {phone}");

            var authResponse = await _supabaseClient.Auth.SignUp(email.Trim(), password, options);
            Console.WriteLine("AuthResponse received");

            if (authResponse.User == null)
            {
                Console.WriteLine(" Signup failed.");
                return null;
            }
            // 🔹 Save session for persistent login
            await SaveSession();

            Console.WriteLine($" User signed up successfully: {authResponse.User.Email}");
            return authResponse.User;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error signing up: {ex.Message}");
            return null;
        }
    }


    // 🔹 Save session manually after login
    public async Task SaveSession()
    {
        var session = _supabaseClient.Auth.CurrentSession;
        if (session != null)
        {
            var sessionJson = JsonSerializer.Serialize(session);
            await File.WriteAllTextAsync("session.json", sessionJson);
            Console.WriteLine(" Session saved.");
        }
    }

    // 🔹 Restore session manually at startup
    public async Task RestoreSession()
    {
        try
        {
            if (File.Exists("session.json"))
            {
                var sessionJson = await File.ReadAllTextAsync("session.json");
                var session = JsonSerializer.Deserialize<Session>(sessionJson);

                if (session != null)
                {
                    await _supabaseClient.Auth.SetSession(session.AccessToken, session.RefreshToken);
                    Console.WriteLine(" Session restored.");
                }
            }
            else
            {
                Console.WriteLine(" No saved session found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Failed to restore session: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _dbConnection?.Close();
        _dbConnection?.Dispose();
        Console.WriteLine(" PostgreSQL connection closed.");
        Console.WriteLine(" Supabase client cleanup completed.");
    }
}
