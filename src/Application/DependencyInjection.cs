using Application.Common.Interfaces;
using Application.EventHandlers;
using Application.EventHandlers.Missions;
using Application.Services.AuthenticationService;
using Application.Services.EmployeeService;
using Application.Services.MissionService;
using Application.Services.NotificationService;
using Application.Services.RoutePlanningService;
using Domain.Missions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IRoutePlanningService, RoutePlanningService>();
        services.AddScoped<IMissionService, MissionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDomainDispatcher, DomainDispatcher>();
        services.AddScoped<IDomainEventHandler<MissionFinishedEvent>, MissionFinishedHandler>();

        return services;
    }
}
