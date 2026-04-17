using Cartiva.Application;
using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Infrastructure;
using Cartiva.Infrastructure.AddressService;
using Cartiva.Infrastructure.EmailServices;
using Cartiva.Infrastructure.ImageServices;
using Cartiva.Infrastructure.PaymentService;
using Cartiva.Infrastructure.Promotions;
using Cartiva.Infrastructure.QrCodeServices;
using Cartiva.Persistence;
using Cartiva.Shared;
using cartivaWeb.HangFire;
using CartivaWeb.Areas.Admin.Controllers;
using CartivaWeb.Routing;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity using ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddRazorPages();
builder.Services.AddScoped<IEmailSender, Cartiva.Infrastructure.EmailServices.EmailSender>();
builder.Services.AddHttpClient<AddressLookupService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
Stripe.StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"];
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<Cartiva.Infrastructure.EmailServices.IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IPromotionService, Cartiva.Infrastructure.Promotions.PromotionService>();

// Bring shipping service typed client
builder.Services.AddHttpClient<Cartiva.Infrastructure.ShippingServices.IBringShippingService, Cartiva.Infrastructure.ShippingServices.BringShippingService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Bring:BaseUrl"] ?? "https://api.bring.com/shipping/api/v1");
    client.DefaultRequestHeaders.Add("Accept", "application/xml");
});
// Hangfire configuration
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices(); // registers ICompanyShipmentApprovalService etc.
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

// ✅ Register job services as Scoped (not Transient)
builder.Services.AddScoped<TestJobService>();
var app = builder.Build();
// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    DbInitializer.Seed(db, userManager, roleManager);
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/test-hangfire", () =>
{
    BackgroundJob.Enqueue<TestJobService>(x => x.RunJob());

    return "Job queued!";
});
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
HangfireJobsInitializer.RegisterRecurringJobs();

app.MapRazorPages();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();