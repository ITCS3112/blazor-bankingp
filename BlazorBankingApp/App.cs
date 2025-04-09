using BlazorBankingApp.Components;
using BlazorBankingApp.Services;  // Import services
using DotNetEnv;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Initialize and register Supabase.Client
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")?.Trim()
    ?? throw new InvalidOperationException("SUPABASE_URL environment variable is not set.");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY")?.Trim()
    ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY environment variable is not set.");

var supabaseOptions = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions);


// ⚡️ Ensure Supabase is fully initialized before registering services
await supabaseClient.InitializeAsync();

// 🛠️ Attempt to refresh session on startup to avoid "No active session found" issues
try
{
    var session = supabaseClient.Auth.CurrentSession;
    if (session != null)
    {
        try
        {
            var user = await supabaseClient.Auth.GetUser(session.AccessToken);
            Console.WriteLine($" Session refreshed for: {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error refreshing session: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine(" No active session found at startup.");
    }
    Console.WriteLine(" Supabase session refreshed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($" Error refreshing Supabase session: {ex.Message}");
}

var supabaseService = new SupabaseService();
await supabaseService.RestoreSession(); // Restore session at startup

builder.Services.AddSingleton(supabaseService);

builder.Services.AddSingleton(supabaseClient);
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSingleton<UserService>();

// Register Blazor Components
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

// Configure HTTP Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// 🌐 Custom Domain Configuration
app.Urls.Add("http://CharlotteBanking:5070");
app.Urls.Remove("http://localhost:5070");

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();