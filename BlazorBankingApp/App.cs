// filepath: /Users/zacharyvogel/Documents/GitHub/blazor-bankingp/BlazorBankingApp/App.cs
using BlazorBankingApp.Components;
using Npgsql;
using System.Threading.Tasks;
using Supabase;
using Npgsql.Internal;
using System.Collections.Generic;
using DotNetEnv; // Add this using directive

/************IMPORTANT***********
    THESE ARE THE DIRECTIONS FOR ADDING THE VARIABLES
    1. In the terminal, change directory to BankingSystem and 
        install nuget package using 'dotnet add package Npgsql'
    2. Copy each of the export commands below and paste them into your terminal
    3. Run the program
***********************************************************************************/


// Load environment variables from .env file
DotNetEnv.Env.Load();

// Debug: Print the SUPABASE_URL to verify it is loaded
Console.WriteLine($"SUPABASE_URL: {Environment.GetEnvironmentVariable("SUPABASE_URL")}");

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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(_ => new Supabase.Client(url, key, options));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();
//app.MapFallbackToPage("/Login"); // Set Login as the default page


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();