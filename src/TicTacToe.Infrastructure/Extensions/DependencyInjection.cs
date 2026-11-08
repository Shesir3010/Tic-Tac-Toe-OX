using Cortex.Mediator;
using Cortex.Mediator.DependencyInjection;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Domain.Contracts;
using TicTacToe.Infrastructure.Data;
using TicTacToe.Infrastructure.Data.Repositories;
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
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPresenceTracker, PresenceTracker>();
        services.AddSingleton<IInvitationManager, InvitationManager>();
        services.AddSingleton<IRoomStore, InMemoryRoomStore>();
        services.Configure<TicTacToe.Application.Settings.RoomSettings>(configuration.GetSection("RoomSettings"));
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

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
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}
