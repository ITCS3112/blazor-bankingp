using BlazorBankingApp.Components;
using BlazorBankingApp.Services;  // Import services
using DotNetEnv;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

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
