using Microsoft.AspNetCore.Identity;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Infrastructure.Identity;

namespace TicTacToe.Infrastructure.Identity;

public sealed class AccountService(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn) : IAccountService
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
}
