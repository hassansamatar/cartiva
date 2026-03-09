using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CartivaWeb.Routing
{
    // Permissive route constraint for legacy templates that mistakenly use :Customer.
    // Returns true for any non-empty value; adjust logic if a stricter rule is required.
    public class CustomerRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values == null) return false;
            if (values.TryGetValue(routeKey, out var value))
            {
                return value != null && !string.IsNullOrEmpty(value.ToString());
            }
            return false;
        }
    }
}
