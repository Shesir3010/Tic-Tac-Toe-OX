using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Web.Models;

namespace TicTacToe.Web.Controllers;

public class AccountController(IAccountService accounts) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accounts.RegisterAsync(model.Username, model.Email, model.Password, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var (code, description) in result.Errors)
            {
                if (code.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase) ||
                    code.Contains("InvalidUserName", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(RegisterModel.Username), "That username is already taken.");
                }
                else if (code.Contains("DuplicateEmail", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(RegisterModel.Email), "That email is already taken.");
                }
                else if (code.Contains("Password", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(RegisterModel.Password), description);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, description);
                }
            }

            return View(model);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View(new LoginModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accounts.LoginAsync(model.UsernameOrEmail, model.Password, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid username or password.");
            return View(model);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Profile));
        var result = await accounts.ChangePasswordAsync(model.CurrentPassword, model.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ProfileError"] = result.ErrorMessage ?? "Could not change password.";
            if (result.ErrorCode == "Auth.WrongPassword")
            {
                TempData["ProfileFieldError"] = "CurrentPassword";
            }
        }
        else
        {
            TempData["ProfileSuccess"] = "Password changed.";
        }
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult Profile() => View(new ChangePasswordModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(DeleteAccountModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.Name is not null && !string.Equals(model.ConfirmUsername, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ProfileError"] = "Type your username to confirm delete.";
            return RedirectToAction(nameof(Profile));
        }
        var result = await accounts.DeleteAccountAsync(model.Password, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ProfileError"] = result.ErrorMessage ?? "Could not delete account.";
            if (result.ErrorCode == "Auth.WrongPassword")
            {
                TempData["ProfileFieldError"] = "Password";
            }
            return RedirectToAction(nameof(Profile));
        }
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await accounts.LogoutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
