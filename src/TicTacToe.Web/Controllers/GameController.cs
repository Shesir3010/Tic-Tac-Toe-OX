using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Game.Moves.Command;
using TicTacToe.Web.Models;

namespace TicTacToe.Web.Controllers;

/// <summary>Thin JSON gateway for local/computer moves (single endpoint per AGENTS). No rules here.</summary>
[ApiController]
[Route("api/game")]
[ValidateAntiForgeryToken]
public class GameController(IMediator mediator) : ControllerBase
{
    [HttpPost("move")]
    public async Task<IActionResult> Move([FromBody] MoveRequest request, CancellationToken cancellationToken)
    {
        var command = new PlayMoveCommand(
            request.Cells, request.MoveIndex, request.Mark, request.Mode, request.Difficulty);
        var result = await mediator.SendAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var statusCode = result.Error!.Code switch
            {
                "Game.InvalidCell" => StatusCodes.Status400BadRequest,
                "Validation.Error" => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status409Conflict,
            };
            return StatusCode(statusCode, new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
