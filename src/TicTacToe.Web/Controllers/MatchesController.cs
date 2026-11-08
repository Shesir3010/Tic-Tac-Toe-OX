using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Matches.History.Command;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Web.Controllers;

/// <summary>Razor POST save for finished local/computer games (M5). Guests cannot reach it ([Authorize]).</summary>
[Authorize]
public class MatchesController(IMediator mediator) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        [FromForm] string moves,
        [FromForm] Player symbol,
        [FromForm] GameMode mode,
        [FromForm] Difficulty? difficulty,
        [FromForm] MatchResult claimedResult,
        Guid? opponentUserId,
        CancellationToken cancellationToken)
    {
        var list = string.IsNullOrWhiteSpace(moves)
            ? []
            : moves.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToArray();

        var result = await mediator.SendAsync(
            new SaveMatchCommand(list, claimedResult, symbol, mode, difficulty, opponentUserId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { saved = true });
    }
}
