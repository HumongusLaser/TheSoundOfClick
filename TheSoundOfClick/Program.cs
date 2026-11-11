using System.Text.Json;
using Npgsql;
using TheSoundOfClick;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using TheSoundOfClick.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Argon2Sharp;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TheSoundOfClick.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
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

builder.Services.AddHttpContextAccessor();

string secretFile = File.ReadAllText("secret.json");

string connectionString;

using (JsonDocument secretJson = JsonDocument.Parse(secretFile))
{
  connectionString = secretJson.RootElement.GetProperty("ConnectionString").GetString()
    ?? throw new InvalidOperationException("The database connection string is missing.");
}

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton(Argon2Parameters.CreateHighSecurity());
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductImageStorage>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DiscountRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<CartRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserLoginRepository>();
builder.Services.AddScoped<PasswordResetRepository>();
builder.Services.AddSingleton<LoginSecurityRepository>();
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<PasswordResetEmailSender>();
builder.Services.AddScoped<LoginApprovalEmailSender>();
builder.Services.AddScoped<LoginApprovalService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<HashingService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped(sp => new HttpClient());
// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews();

var app = builder.Build();

await app.Services.GetRequiredService<LoginSecurityRepository>().EnsureCreatedAsync();
await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
  await scope.ServiceProvider.GetRequiredService<ProductRepository>().EnsureImageTableCreatedAsync();
}
string productImageDirectory = Path.Combine(builder.Environment.ContentRootPath, "Image");
Directory.CreateDirectory(productImageDirectory);

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
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(productImageDirectory),
    RequestPath = "/Image"
});

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();
