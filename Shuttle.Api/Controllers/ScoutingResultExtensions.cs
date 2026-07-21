using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Services.Scouting;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Maps <see cref="ScoutingResult"/>/<see cref="ScoutingResult{T}"/> service outcomes to HTTP
/// responses, keeping scouting controllers free of repetitive status-code plumbing.
/// </summary>
internal static class ScoutingResultExtensions {
    public static ActionResult<T> ToActionResult<T>(this ScoutingResult<T> result, ControllerBase controller) =>
        result.Outcome == ScoutingOutcome.Ok
            ? controller.Ok(result.Value!)
            : Error(controller, result.Outcome, result.Error);

    public static ActionResult ToNoContent(this ScoutingResult result, ControllerBase controller) =>
        result.Outcome == ScoutingOutcome.Ok
            ? controller.NoContent()
            : Error(controller, result.Outcome, result.Error);

    private static ActionResult Error(ControllerBase controller, ScoutingOutcome outcome, string? error) {
        var (status, title) = outcome switch {
            ScoutingOutcome.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ScoutingOutcome.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ScoutingOutcome.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ScoutingOutcome.Invalid => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status400BadRequest, "Invalid request"),
        };

        return controller.StatusCode(status, new ProblemDetails {
            Title = title,
            Detail = error,
            Status = status,
        });
    }
}
