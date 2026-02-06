
using RestaurantDashboard.Components;
using RestaurantDashboard.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register specialized analytics services
builder.Services.AddScoped<ISalesAnalyticsService, SalesAnalyticsService>();
builder.Services.AddScoped<IMenuItemAnalyticsService, MenuItemAnalyticsService>();
builder.Services.AddScoped<IInventoryAnalyticsService, InventoryAnalyticsService>();
builder.Services.AddScoped<ILocationAnalyticsService, LocationAnalyticsService>();

// Register main Analytics Service facade
builder.Services.AddScoped<AnalyticsService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
