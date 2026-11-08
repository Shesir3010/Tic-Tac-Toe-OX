using Cortex.Mediator;
using Cortex.Mediator.DependencyInjection;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.Contracts;
using TicTacToe.Infrastructure.Data;
using TicTacToe.Infrastructure.Identity;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // CQRS + mapping: handlers/profiles live in Application, registration (DI) lives here.
        services.AddCortexMediator(
            new[] { typeof(ApplicationAssemblyMarker) },
            options => { options.AddDefaultBehaviors(); });
        services.AddMapster();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRandomSource, SystemRandomSource>();
        services.AddScoped<IComputerPlayerFactory, ComputerPlayerFactory>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
