using Serilog;
using TicTacToe.Application;
using TicTacToe.Infrastructure.Extensions;
using TicTacToe.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(path: "logs/log-.txt", rollingInterval: Serilog.RollingInterval.Day);
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
// AddInfrastructure registers DbContext + Identity (cookie auth, ApplicationScheme) + TimeProvider.
// Identity cookie is the sole credential for pages AND hub (ADR-0004); no JWT, no manual AddCookie here.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorization();

// Development validates scopes by default via WebApplicationBuilder (ValidateScopes = IsDevelopment).
// No extra registration needed; see system-architecture.md S4.

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health");
app.MapHub<GameHub>("/hubs/game");

app.Run();
