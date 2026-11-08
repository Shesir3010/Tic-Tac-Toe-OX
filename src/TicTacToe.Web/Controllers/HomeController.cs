using System.Diagnostics;
using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Matches.History.Query;
using TicTacToe.Web.Models;

namespace TicTacToe.Web.Controllers;

public class HomeController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var stats = await mediator.SendAsync(new GetStatsQuery(), cancellationToken);
            ViewBag.Stats = stats.Value;
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
