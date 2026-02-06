using RestaurantApi.Data.Repositories;
using RestaurantApi.Services;
using RestaurantApi.Services.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Configure file logging
var logDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RestaurantApi",
    "Logs");

if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

var logFilePath = Path.Combine(logDirectory, $"api-{DateTime.Now:yyyy-MM-dd}.log");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));
builder.Logging.SetMinimumLevel(LogLevel.Information);

Console.WriteLine($"API logs are being written to: {logFilePath}");

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Restaurant Event API",
        Version = "v1",
        Description = "Event-based API for processing POS events (orders, inventory, payments)",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Restaurant API"
        }
    });
});

// Register Dapper repositories
builder.Services.AddScoped(sp => new ProcessedEventRepository(connectionString));
builder.Services.AddScoped(sp => new InventoryItemRepository(connectionString));
builder.Services.AddScoped(sp => new LocationInventoryRepository(connectionString));
builder.Services.AddScoped(sp => new InventoryCostRecordRepository(connectionString));
builder.Services.AddScoped(sp => new InventoryQueryRepository(connectionString));
builder.Services.AddScoped(sp => new OrderRepository(connectionString));
builder.Services.AddScoped(sp => new OrderLineRepository(connectionString));
builder.Services.AddScoped(sp => new PaymentRepository(connectionString));
builder.Services.AddScoped(sp => new CashTransactionRepository(connectionString));
builder.Services.AddScoped(sp => new MenuItemRepository(connectionString));
builder.Services.AddScoped(sp => new MenuItemComponentRepository(connectionString));
builder.Services.AddScoped(sp => new ShiftRepository(connectionString));

// Register repository interfaces
builder.Services.AddScoped<IProcessedEventRepository>(sp => sp.GetRequiredService<ProcessedEventRepository>());
builder.Services.AddScoped<IInventoryItemRepository>(sp => sp.GetRequiredService<InventoryItemRepository>());
builder.Services.AddScoped<ILocationInventoryRepository>(sp => sp.GetRequiredService<LocationInventoryRepository>());
builder.Services.AddScoped<IInventoryCostRecordRepository>(sp => sp.GetRequiredService<InventoryCostRecordRepository>());
builder.Services.AddScoped<IInventoryQueryRepository>(sp => sp.GetRequiredService<InventoryQueryRepository>());
builder.Services.AddScoped<IOrderRepository>(sp => sp.GetRequiredService<OrderRepository>());
builder.Services.AddScoped<IOrderLineRepository>(sp => sp.GetRequiredService<OrderLineRepository>());
builder.Services.AddScoped<IPaymentRepository>(sp => sp.GetRequiredService<PaymentRepository>());
builder.Services.AddScoped<ICashTransactionRepository>(sp => sp.GetRequiredService<CashTransactionRepository>());
builder.Services.AddScoped<IMenuItemRepository>(sp => sp.GetRequiredService<MenuItemRepository>());
builder.Services.AddScoped<IMenuItemComponentRepository>(sp => sp.GetRequiredService<MenuItemComponentRepository>());
builder.Services.AddScoped<IShiftRepository>(sp => sp.GetRequiredService<ShiftRepository>());

// Register event handlers
builder.Services.AddScoped<IEventHandler, InventoryEventHandler>();
builder.Services.AddScoped<IEventHandler, OrderEventHandler>();
builder.Services.AddScoped<IEventHandler, PaymentEventHandler>();
builder.Services.AddScoped<IEventHandler, CashTransactionEventHandler>();
builder.Services.AddScoped<IEventHandler, ShiftEventHandler>();

// Register event processor
builder.Services.AddScoped<IEventProcessor, EventProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant Event API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

await app.RunAsync();
