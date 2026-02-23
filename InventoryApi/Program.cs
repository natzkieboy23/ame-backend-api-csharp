using InventoryApi.Data;
using InventoryApi.QuickBooks;
using Microsoft.EntityFrameworkCore;

// ── QuickBooks sync console mode ───────────────────────────────────────────────
// Run with:  dotnet run -- --sync
if (args.Contains("--sync"))
{
    await SyncConsole.RunAsync();
    return;
}

// ── Web API mode ───────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Enables running as a Windows Service (sc.exe start/stop).
// Has no effect when run normally from the command line.
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "AME Inventory API";
});

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(5, 7, 32));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "Inventory Management API",
        Version     = "v1",
        Description = "RESTful CRUD API for inventory sites, items, and vendors."
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.MapControllers();
app.Run();
