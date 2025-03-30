namespace BlazorBankingApp.Services;

using Npgsql;
using Supabase;
using Supabase.Gotrue;
using Supabase.Postgrest;
//using Postgrest.Models;
//using Postgrest.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;


public class UserService
{
    public string? id { get; set; }
    public string? email { get; set; }
    public string? authoritylevel { get; set; }
    public string? name { get; set; }
    public string? phone { get; set; }
    public string? password { get; set; }
    public float balance { get; set; }

    private readonly Supabase.Client _supabaseClient;

    public Supabase.Gotrue.User? CurrentUser { get; private set; }

    public UserService(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task LoadBankUser()
    {
        // Load the full user data from Supabase
        var (user, bankUser) = await GetFullUserData();
        if (user == null || bankUser == null)
        {
            Console.WriteLine("Failed to load user data.");
            return;
        }

        // Set properties based on retrieved data
        id = user.Id;
        email = user.Email;
        name = user.UserMetadata.ContainsKey("name") ? user.UserMetadata["name"]?.ToString() : "Unknown";
        phone = user.UserMetadata.ContainsKey("phone") ? user.UserMetadata["phone"]?.ToString() : "Unknown";
        balance = (float)bankUser.Balance;
        authoritylevel = bankUser.AuthorityLevel;

        CurrentUser = user;
        Console.WriteLine($"Loaded user: {email}, Name: {name}, Phone: {phone}, Balance: {balance}, Authority Level: {authoritylevel}");
    }

    private async Task<(Supabase.Gotrue.User?, BankUser?)> GetFullUserData()
    {
        var session = _supabaseClient.Auth.CurrentSession;
        if (session == null) throw new InvalidOperationException("No active session found.");
        var user = await _supabaseClient.Auth.GetUser(session.AccessToken);
        if (user == null) return (null, null);

        // Extract metadata
        var name = user.UserMetadata.ContainsKey("name") ? user.UserMetadata["name"]?.ToString() : "Unknown";
        var phone = user.UserMetadata.ContainsKey("phone") ? user.UserMetadata["phone"]?.ToString() : "Unknown";

        var bankUser = await _supabaseClient
            .From<BankUser>()
            .Select("*")
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id)
            .Single();

        Console.WriteLine($"User: {user.Email}, Name: {name}, Phone: {phone}, Balance: {bankUser?.Balance}");

        return (user, bankUser);
    }

    private async Task GetBankUserData(){
        var (user, bankUser) = await GetFullUserData();
        if (user == null || bankUser == null)
        {
            Console.WriteLine("Failed to retrieve user data.");
            return;
        }

        // Set properties based on retrieved data
        id = user.Id;
        balance = (float)bankUser.Balance;
        authoritylevel = bankUser.AuthorityLevel;

    }
}


public class SupabaseService : IDisposable
{
    private readonly Supabase.Client _supabaseClient;
    private readonly NpgsqlConnection _dbConnection;

    public SupabaseService()
    {
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL")?.Trim() ?? throw new InvalidOperationException("SUPABASE_URL environment variable is not set.");
        var key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")?.Trim() ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY environment variable is not set.");
        string databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? throw new InvalidOperationException("DATABASE_URL environment variable is not set.");

        var options = new SupabaseOptions { AutoConnectRealtime = true };
        _supabaseClient = new Supabase.Client(url, key, options);

        Task.Run(async () => await _supabaseClient.InitializeAsync()).Wait();
        Console.WriteLine("Supabase Client initialized.");

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        string connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";
        _dbConnection = new NpgsqlConnection(connectionString);
        _dbConnection.Open();
        Console.WriteLine("PostgreSQL connection opened.");
    }

    public Supabase.Client GetClient() => _supabaseClient;
    public async Task<Supabase.Gotrue.User?> SignUpUser(string email, string password, string name, string phone)
    {
        try
        {
            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object?>
                {
                    { "name", name },
                    { "phone", phone }
                }
            };

            var authResponse = await _supabaseClient.Auth.SignUp(email.Trim(), password, options);

            if (authResponse.User == null)
            {
                Console.WriteLine("Signup failed: " + authResponse);
            }

            return authResponse.User;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error signing up: " + ex.Message);
            return null;
        }
    }


    public NpgsqlConnection GetConnection() => _dbConnection;

    public void Dispose()
    {
        _dbConnection?.Close();
        _dbConnection?.Dispose();
        Console.WriteLine("PostgreSQL connection closed.");
        Console.WriteLine("Supabase client cleanup completed.");
    }
}
