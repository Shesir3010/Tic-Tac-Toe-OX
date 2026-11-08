using Microsoft.AspNetCore.Identity;

namespace TicTacToe.Infrastructure.Identity;

// UserName is the display name (owner decision 2026-11-08, no separate DisplayName column).
// See ADR-0004 and schema-and-erd.md. Regex ^[A-Za-z0-9_]{3,20}$ enforced in-handler, not here.
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }
}
