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
await supabaseClient.InitializeAsync();

builder.Services.AddSingleton(supabaseClient);

// Dependency Injection
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

// Configure HTTP Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.Urls.Add("http://CharlotteBanking:5070");
app.Urls.Remove("http://localhost:5070");

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();