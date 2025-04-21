using BlazorBankingApp.Components;
using BlazorBankingApp.Services;
using DotNetEnv;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 🔹 Register SupabaseService as a singleton
builder.Services.AddSingleton<SupabaseService>();

// 🔹 Register your other services that depend on Supabase
builder.Services.AddSingleton<UserService>();

// 🔹 Add Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();


// 🌐 Restore Supabase session before app starts (optional but recommended)
var supabaseService = app.Services.GetRequiredService<SupabaseService>();
await supabaseService.RestoreSession(); // Load session from file if it exists


// 🔧 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
