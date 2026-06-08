using Cartiva.Application.Abstractions;
using Cartiva.Application.Interfaces;
using Cartiva.Application.Services;
using Cartiva.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Cartiva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Shipment services
        services.AddScoped<ICompanyShipmentApprovalService, CompanyShipmentApprovalService>();
        services.AddScoped<ICompanyShipmentProcessingService, CompanyShipmentProcessingService>();
        services.AddScoped<IShipmentService, ShipmentService>();

        // Invoice services
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ICreditNoteService, CreditNoteService>();
        services.AddScoped<IAccountsReceivableAdjustmentService, AccountsReceivableAdjustmentService>();

        // Product services
        services.AddScoped<IProductService, ProductService>();

        // Cart services
        services.AddScoped<ICartService, CartService>();

        // Order services
        services.AddScoped<IOrderService, OrderService>();

        // Payment services (NEW: Modularized abstraction)
        services.AddScoped<IPaymentService, PaymentService>();

        // Category services
        services.AddScoped<ICategoryService, CategoryService>();

        // Company services
        services.AddScoped<ICompanyService, CompanyService>();

        // Review services
        services.AddScoped<IReviewService, ReviewService>();

        // Return services
        services.AddScoped<IReturnService, ReturnService>();

        // User services
        services.AddScoped<IUserService, UserService>();

        // Home/Browsing services
        services.AddScoped<IHomeService, HomeService>();

        // Notification services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChannelResolver, ChannelResolver>();

        return services;
    }
}