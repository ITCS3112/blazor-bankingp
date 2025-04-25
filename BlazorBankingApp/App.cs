using BlazorBankingApp.Components;
<<<<<<< HEAD
using BlazorBankingApp.Service;
using BlazorBankingApp.Services;  // Import services
=======
using BlazorBankingApp.Services;
>>>>>>> eb70c34 (Logout page created, Loan page works)
using DotNetEnv;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 🔹 Register SupabaseService as a singleton
builder.Services.AddSingleton<SupabaseService>();

// 🔹 Register your other services that depend on Supabase
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<TransactionsService>();

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
