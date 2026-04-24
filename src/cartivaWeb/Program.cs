using Cartiva.Application;
using Cartiva.Domain;
using Cartiva.Infrastructure;
using Cartiva.Persistence;
using Cartiva.Shared.Configuration;
using cartivaWeb.HangFire;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// Configuration Binding
// ===========================================
builder.Services.Configure<CartivaContact>(builder.Configuration.GetSection("CartivaContact"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.Configure<InvoiceSettings>(builder.Configuration.GetSection(InvoiceSettings.SectionName));

// Register CartivaContact as singleton for direct injection
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<CartivaContact>>().Value);

// ===========================================
// Database Context
// ===========================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===========================================
// Identity Configuration
// ===========================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// ===========================================
// MVC & Razor Pages
// ===========================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// ===========================================
// Application & Infrastructure Services (Clean Architecture)
// ===========================================
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ===========================================
// Hangfire (Background Jobs)
// ===========================================
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

// Hangfire job services
builder.Services.AddScoped<TestJobService>();

// ===========================================
// Logging
// ===========================================
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Application starting...");
logger.LogInformation("📧 Email: {Email}", builder.Configuration["EmailSettings:SenderEmail"]);
logger.LogInformation("🔧 SMTP: {Server}:{Port}", 
    builder.Configuration["EmailSettings:SmtpServer"],
    builder.Configuration["EmailSettings:SmtpPort"]);

// Check if NotificationWorker is registered
var hostedServices = app.Services.GetServices<IHostedService>();
var workerRegistered = hostedServices.Any(s => s.GetType().Name.Contains("NotificationWorker"));
logger.LogInformation(workerRegistered 
    ? "✅ NotificationWorker IS registered as hosted service" 
    : "❌ NotificationWorker NOT registered!");

// ===========================================
// Database Seeding
// ===========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    DbInitializer.Seed(db, userManager, roleManager);
}

// ===========================================
// Middleware Pipeline
// ===========================================

// Add diagnostic endpoint for notification system (Development only)
if (app.Environment.IsDevelopment())
{
    // Test direct SMTP connection
    app.MapGet("/test-smtp-direct", async (IConfiguration config, ILogger<Program> logger) =>
    {
        try
        {
            logger.LogInformation("Testing direct SMTP connection...");

            var smtpServer = config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(config["EmailSettings:SmtpPort"] ?? "587");
            var senderEmail = config["EmailSettings:SenderEmail"];
            var senderName = config["EmailSettings:SenderName"];
            var password = (config["EmailSettings:Password"] ?? string.Empty).Replace(" ", string.Empty);
            var enableSsl = bool.Parse(config["EmailSettings:EnableSsl"] ?? "true");

            logger.LogInformation("SMTP Config: {Server}:{Port}, From: {Email}, SSL: {Ssl}", 
                smtpServer, smtpPort, senderEmail, enableSsl);

            using var smtp = new System.Net.Mail.SmtpClient(smtpServer, smtpPort)
            {
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(senderEmail, password),
                EnableSsl = enableSsl
            };

            var message = new System.Net.Mail.MailMessage(
                senderEmail,
                senderEmail,  // Send to yourself for testing
                "Test Email from Cartiva",
                "This is a direct SMTP test. If you receive this, SMTP is working!")
            {
                IsBodyHtml = false
            };

            logger.LogInformation("Sending test email to {Email}...", senderEmail);
            await smtp.SendMailAsync(message);
            logger.LogInformation("✅ Email sent successfully!");

            return Results.Ok(new
            {
                Success = true,
                Message = "Email sent successfully via SMTP!",
                SmtpServer = smtpServer,
                SmtpPort = smtpPort,
                From = senderEmail,
                To = senderEmail
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ SMTP test failed");
            return Results.Ok(new
            {
                Success = false,
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    });

    app.MapGet("/test-order-confirmation/{orderId?}", async (
        int? orderId,
        ApplicationDbContext db,
        Cartiva.Infrastructure.Notifications.Interfaces.ITemplateRenderer templateRenderer,
        Cartiva.Infrastructure.Notifications.Interfaces.ISmtpEmailSender smtpEmailSender,
        ILogger<Program> logger) =>
    {
        try
        {
            var order = orderId.HasValue
                ? await db.OrderHeaders
                    .Include(o => o.ApplicationUser)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value)
                : await db.OrderHeaders
                    .Include(o => o.ApplicationUser)
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();

            if (order == null)
            {
                return Results.NotFound(new { Success = false, Message = "No order found to test." });
            }

            if (string.IsNullOrWhiteSpace(order.ApplicationUser?.Email))
            {
                return Results.BadRequest(new { Success = false, Message = "Order user does not have an email address." });
            }

            var model = new Cartiva.Infrastructure.Templates.Models.OrderConfirmationModel
            {
                OrderId = order.Id.ToString(),
                Name = string.IsNullOrWhiteSpace(order.ApplicationUser.Name) ? order.Name : order.ApplicationUser.Name,
                OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                TotalAmount = order.OrderTotal.ToString("C")
            };

            var html = await templateRenderer.RenderAsync("OrderConfirmation", model);
            var sent = await smtpEmailSender.SendEmailAsync(
                order.ApplicationUser.Email,
                $"Order Confirmation - Order #{order.Id}",
                html);

            return Results.Ok(new
            {
                Success = sent,
                OrderId = order.Id,
                Recipient = order.ApplicationUser.Email,
                Name = model.Name,
                Message = sent
                    ? "Order confirmation test email sent directly via template renderer + SMTP."
                    : "SMTP send returned false. Check logs."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send direct order confirmation test email");
            return Results.Ok(new
            {
                Success = false,
                Error = ex.Message,
                InnerError = ex.InnerException?.Message
            });
        }
    });

    app.MapGet("/test-notification", async (
        Cartiva.Domain.Interfaces.INotificationService notificationService,
        ILogger<Program> logger) =>
    {
        try
        {
            logger.LogInformation("Creating test notification...");

            var notificationId = await notificationService.SendAsync(new Cartiva.Domain.Interfaces.NotificationRequest(
                Recipient: "hornafricanorway@gmail.com",  // Your email
                Type: Cartiva.Domain.Enums.NotificationType.WelcomeEmail,
                TemplateData: new Dictionary<string, object>
                {
                    ["userId"] = "test-user",
                    ["name"] = "Test User",
                    ["email"] = "hornafricanorway@gmail.com",
                    ["isActive"] = true.ToString(),
                    ["verificationLink"] = "https://example.com"
                },
                Subject: "Test Notification from Cartiva"
            ));

            logger.LogInformation("✅ Notification {Id} created and enqueued", notificationId);

            return Results.Ok(new
            {
                Success = true,
                NotificationId = notificationId,
                Message = "Notification created and enqueued. Check console logs for '📨 Dequeued notification' message.",
                Instructions = "Now check: 1) Console logs, 2) Database Notifications table"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to create notification");
            return Results.Ok(new
            {
                Success = false,
                Error = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    });

    app.MapGet("/check-worker", (IServiceProvider services, ILogger<Program> logger) =>
    {
        logger.LogInformation("Checking for hosted services...");

        var hostedServices = services.GetServices<IHostedService>();
        var workerTypes = hostedServices.Select(s => s.GetType().Name).ToList();

        logger.LogInformation("Found {Count} hosted services: {Services}", 
            workerTypes.Count, string.Join(", ", workerTypes));

        return Results.Ok(new
        {
            HostedServicesCount = hostedServices.Count(),
            HostedServices = workerTypes,
            HasNotificationWorker = workerTypes.Any(t => t.Contains("NotificationWorker")),
            Message = workerTypes.Any(t => t.Contains("NotificationWorker")) 
                ? "✅ NotificationWorker is registered!" 
                : "❌ NotificationWorker NOT found!"
        });
    });

    app.MapGet("/check-templates", (IWebHostEnvironment env, ILogger<Program> logger) =>
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var templatesPath = Path.Combine(basePath, "Templates");

            logger.LogInformation("Checking templates in: {Path}", templatesPath);

            if (!Directory.Exists(templatesPath))
            {
                return Results.Ok(new
                {
                    Success = false,
                    Message = "❌ Templates directory not found!",
                    ExpectedPath = templatesPath,
                    BasePath = basePath
                });
            }

            var templates = Directory.GetFiles(templatesPath, "*.cshtml", SearchOption.AllDirectories)
                .Select(f => Path.GetFileName(f))
                .ToList();

            logger.LogInformation("Found {Count} templates", templates.Count);

            return Results.Ok(new
            {
                Success = true,
                Message = templates.Any() ? "✅ Templates found!" : "❌ No templates found!",
                TemplatesPath = templatesPath,
                TemplateCount = templates.Count,
                Templates = templates
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking templates");
            return Results.Ok(new
            {
                Success = false,
                Error = ex.Message
            });
        }
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Admin only)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
HangfireJobsInitializer.RegisterRecurringJobs();

// Test endpoint for Hangfire (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test-hangfire", () =>
    {
        BackgroundJob.Enqueue<TestJobService>(x => x.RunJob());
        return "Job queued!";
    });
}

// ===========================================
// Routing
// ===========================================
app.MapRazorPages();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();