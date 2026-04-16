using Cartiva.Application.Abstractions;
using Cartiva.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICompanyShipmentApprovalService, Services.CompanyShipmentApprovalService>();
        // other application services...
        services.AddScoped<ICompanyShipmentProcessingService, CompanyShipmentProcessingService>();
        return services;
    }
}