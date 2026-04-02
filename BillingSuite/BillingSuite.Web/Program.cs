using BillingSuite.Infrastructure.DependencyInjection;
using BillingSuite.Infrastructure.Logging;
using BillingSuite.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.ConfigureSerilog();

// Register InvoiceSettings from configuration
builder.Services.Configure<InvoiceSettings>(builder.Configuration.GetSection("InvoiceSettings"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=BillingSuiteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
//.AddRazorRuntimeCompilation(); // optional hot reload for Razor

var app = builder.Build();

// Log application startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting in {Environment} environment", app.Environment.EnvironmentName);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/", () => Results.Redirect("/Home"));

logger.LogInformation("Application started successfully");

try
{
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("Application shutting down");
}