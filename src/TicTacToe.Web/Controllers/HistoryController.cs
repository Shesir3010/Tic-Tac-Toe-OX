using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Matches.History.Query;
using TicTacToe.Web.Models;

namespace TicTacToe.Web.Controllers;

[Authorize]
public class HistoryController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var history = await mediator.SendAsync(new GetHistoryQuery(page), cancellationToken);
        var stats = await mediator.SendAsync(new GetStatsQuery(), cancellationToken);

        return View(new HistoryViewModel(
            history.IsSuccess ? history.Value ?? [] : [],
            history.Error?.Message ?? string.Empty,
            stats.Value,
            page));
    }
}
