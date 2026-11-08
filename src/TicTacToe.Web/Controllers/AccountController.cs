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

        return RedirectToAction(nameof(HistoryController.Index), "History");
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

        return RedirectToAction(nameof(HistoryController.Index), "History");
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
