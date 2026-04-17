using Hangfire.Dashboard;

namespace cartivaWeb.HangFire
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var env = httpContext.RequestServices.GetService<IWebHostEnvironment>();

            // ✅ Safe null check
            if (env?.IsDevelopment() == true)
                return true;

            return httpContext.User.Identity?.IsAuthenticated == true &&
                   httpContext.User.IsInRole("Admin");
        }
    }
}