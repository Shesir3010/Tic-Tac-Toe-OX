using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.Features.Rooms.Online.Query;

namespace TicTacToe.Web.Controllers;

/// <summary>Online room page — membership checked by GetRoomQuery (non-players get the error view).</summary>
[Authorize]
public sealed class RoomsController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Index(Guid id)
    {
        var result = await mediator.SendAsync(new GetRoomQuery(id));
        if (!result.IsSuccess || result.Value is null)
        {
            return View("NotFoundRoom", result.Error?.Message ?? "Room not found.");
        }
        ViewBag.RoomJson = System.Text.Json.JsonSerializer.Serialize(result.Value);
        return View(result.Value);
    }
}
