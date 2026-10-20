using System.Text.Json;
using Npgsql;
using TheSoundOfClick;
using TheSoundOfClick.Components;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();
