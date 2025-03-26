namespace BlazorBankingApp.Services;

using Npgsql;
using Supabase;

public class UserService
{
    public string? id { get; set; }
    public string? email { get; set; }
    public string? authoritylevel { get; set; }
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
    public NpgsqlConnection GetConnection() => _dbConnection;

    public void Dispose()
    {
        _dbConnection?.Close();
        _dbConnection?.Dispose();
        Console.WriteLine("PostgreSQL connection closed.");
        Console.WriteLine("Supabase client cleanup completed.");
    }
}
