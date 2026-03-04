using BillingSuite.Infrastructure.DependencyInjection;
using BillingSuite.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Register InvoiceSettings from configuration
builder.Services.Configure<InvoiceSettings>(builder.Configuration.GetSection("InvoiceSettings"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=BillingSuiteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
//.AddRazorRuntimeCompilation(); // optional hot reload for Razor

var app = builder.Build();


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

app.Run();