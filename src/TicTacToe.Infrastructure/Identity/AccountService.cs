using Cortex.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Online.Command;
using TicTacToe.Infrastructure.Data;
using TicTacToe.Infrastructure.Identity;

namespace TicTacToe.Infrastructure.Identity;

public sealed class AccountService(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    ICurrentUser currentUser,
    IRoomStore rooms,
    ApplicationDbContext db,
    IMediator mediator) : IAccountService
{
    public async Task<AccountRegistrationResult> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username.Trim(),
            Email = email.Trim(),
        };
        var identityResult = await users.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            return AccountRegistrationResult.Failed(identityResult.Errors.Select(e => (e.Code, e.Description)));
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return AccountRegistrationResult.Success();
    }

    public async Task<AccountLoginResult> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken)
    {
        var input = usernameOrEmail.Trim();
        var user = (await users.FindByNameAsync(input)) ?? await users.FindByEmailAsync(input);
        if (user is null)
        {
            return new AccountLoginResult(false, false, "Invalid username or password.");
        }

        if (await users.IsLockedOutAsync(user))
        {
            return new AccountLoginResult(false, true, "Too many failed attempts. Try again later.");
        }

        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await users.ResetAccessFailedCountAsync(user);
            return new AccountLoginResult(true, false, null);
        }

        if (result.IsLockedOut)
        {
            return new AccountLoginResult(false, true, "Too many failed attempts. Try again later.");
        }

        return new AccountLoginResult(false, false, "Invalid username or password.");
    }

    public Task LogoutAsync() => signIn.SignOutAsync();

    public async Task<AccountPasswordResult> ChangePasswordAsync(string? currentPassword, string? newPassword, CancellationToken cancellationToken)
    {
        var user = await CurrentSignedInUserAsync();
        if (user is null) return AccountPasswordResult.Failed("Login is required.");
        if (!await users.CheckPasswordAsync(user, currentPassword ?? ""))
        {
            return AccountPasswordResult.WrongPassword();
        }
        var changed = await users.ChangePasswordAsync(user, currentPassword!, newPassword ?? "");
        if (!changed.Succeeded)
        {
            return AccountPasswordResult.Failed(changed.Errors.FirstOrDefault()?.Description ?? "Could not change password.");
        }
        await signIn.RefreshSignInAsync(user);
        return AccountPasswordResult.Success();
    }

    public async Task<AccountPasswordResult> DeleteAccountAsync(string? password, CancellationToken cancellationToken)
    {
        var user = await CurrentSignedInUserAsync();
        if (user is null) return AccountPasswordResult.Failed("Login is required.");
        if (!await users.CheckPasswordAsync(user, password ?? ""))
        {
            return AccountPasswordResult.WrongPassword();
        }

        // Forfeit-first: any Playing room ends as a loss (both rows saved) before the cascade.
        foreach (var roomId in rooms.GetPlayingRoomIds(user.Id))
        {
            await mediator.SendAsync(new LeaveRoomCommand(roomId), cancellationToken);
        }
        // Opponent references: SQL is NoAction for OpponentUserId, so null them explicitly
        // or the cascade of the user's own rows still leaves a dangling FK (1785). History then shows "Deleted player".
        await db.Matches.Where(m => m.OpponentUserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.OpponentUserId, (Guid?)null), cancellationToken);
        var deleted = await users.DeleteAsync(user);
        if (!deleted.Succeeded)
        {
            return AccountPasswordResult.Failed(deleted.Errors.FirstOrDefault()?.Description ?? "Could not delete account.");
        }
        await signIn.SignOutAsync();
        return AccountPasswordResult.Success();
    }

    private async Task<ApplicationUser?> CurrentSignedInUserAsync()
    {
        var id = currentUser.UserId;
        return id is null ? null : await users.FindByIdAsync(id.Value.ToString());
    }
}
