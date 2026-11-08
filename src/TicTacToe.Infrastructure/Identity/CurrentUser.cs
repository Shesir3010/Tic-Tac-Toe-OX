using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TicTacToe.Application.Contracts.Services;

namespace TicTacToe.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : null;
}
