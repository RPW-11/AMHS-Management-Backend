using Application.Common.Interfaces;
using Application.Common.Interfaces.Authentication;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.RoutePlanning.Rgv;
using Infrastructure.Security;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, ConfigurationManager configuration)
    {

        // register the config
        //  Jwt config
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        //  Postgres config
        string? postgresConnectionStr = configuration.GetConnectionString("PostgresConnectionString");

        if (postgresConnectionStr == null)
        {
            throw new Exception("No connection string");
        }

        services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(postgresConnectionStr, 
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // register all of your infranstructure dependencies
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRgvRoutePlanning, RgvRoutePlanning>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IMissionRepository, MissionRepository>();
        return services;
    }
}

