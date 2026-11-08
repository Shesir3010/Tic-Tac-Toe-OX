using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicTacToe.Web.Controllers;

/// <summary>Public (guests included), browser-only preferences (FR-22). No DB, no server round-trip for values.</summary>
[AllowAnonymous]
public class SettingsController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
