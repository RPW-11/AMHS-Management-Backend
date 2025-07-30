using Application.Services.AuthenticationService;
using Application.Services.EmployeeService;
using Application.Services.MissionService.RoutePlanningService;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IRoutePlanningService, RoutePlanningService>();

        return services;
    }
}
