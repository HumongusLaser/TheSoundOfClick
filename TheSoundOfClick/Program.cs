using System.Text.Json;
using Npgsql;
using TheSoundOfClick;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using TheSoundOfClick.Components;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TheSoundOfClick.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Rate limiting til produktsiden
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("products", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddScoped<ProductService>();


string secretFile = File.ReadAllText("secret.json");

string connectionString;

using (JsonDocument secretJson = JsonDocument.Parse(secretFile))
{
  connectionString = secretJson.RootElement.GetProperty("ConnectionString").GetString()
    ?? throw new InvalidOperationException("The database connection string is missing.");
}

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<DiscountRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<UserLoginRepository>();
builder.Services.AddScoped<HashingService>();
builder.Services.AddScoped<LoginService>();

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();
