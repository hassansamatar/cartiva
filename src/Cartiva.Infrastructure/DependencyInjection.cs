using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.AddressService;
using Cartiva.Infrastructure.ImageServices;
using Cartiva.Infrastructure.Notifications;
using Cartiva.Infrastructure.Notifications.Channels;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Infrastructure.Notifications.Queue;
using Cartiva.Infrastructure.Notifications.Templates;
using Cartiva.Infrastructure.PaymentService;
using Cartiva.Infrastructure.Promotions;
using Cartiva.Infrastructure.QrCodeServices;
using Cartiva.Infrastructure.ShippingServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cartiva.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services including email, payments, shipping, and more.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Image services
        services.AddScoped<IImageService, ImageService>();

        // QR Code services
        services.AddScoped<IQrCodeService, QrCodeService>();

        // Promotion services
        services.AddScoped<IPromotionService, PromotionService>();

        // ===========================================
        // Payment Services (Modularized)
        // ===========================================

        // Configure Stripe settings
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));

        // Register Stripe as the payment provider
        services.AddScoped<IPaymentProvider, StripePaymentProvider>();

        // Legacy webhook service (still needed for Hangfire background processing)
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();

        // Configure Stripe API key (legacy, but kept for backward compatibility)
        var stripeSecretKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
        }

        // Address lookup service (HTTP client)
        services.AddHttpClient<AddressLookupService>();

        // Bring shipping service (typed HTTP client)
        services.AddHttpClient<IBringShippingService, BringShippingService>((serviceProvider, client) =>
        {
            var baseUrl = configuration["Bring:BaseUrl"] ?? "https://api.bring.com/shipping/api/v1";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/xml");
        });

        // ===========================================
        // Notification System
        // ===========================================

        // Queue (Singleton for shared queue across app)
        services.AddSingleton<INotificationQueue, NotificationQueue>();

        // Template renderer
        services.AddSingleton<ITemplateRenderer, RazorLightTemplateRenderer>();

        // SMTP sender
        services.AddScoped<ISmtpEmailSender, SmtpEmailSender>();

        // Notification channels
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, SmsNotificationChannel>();

        // Channel resolver (must be scoped due to channel dependencies)
        services.AddScoped<ChannelResolver>();

        // Background worker
        services.AddHostedService<NotificationWorker>();

        return services;
    }
}
