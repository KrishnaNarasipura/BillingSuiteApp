using BillingSuite.Application.Abstractions;
using BillingSuite.Infrastructure.Persistence;
using BillingSuite.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingSuite.Infrastructure.DependencyInjection
{

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<BillingDbContext>(opt =>
                opt.UseSqlServer(connectionString));

            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICompanySettingsService, CompanySettingsService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ITaxSettingsService, TaxSettingsService>();

            return services;
        }
    }

}
