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

var app = builder.Build();

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