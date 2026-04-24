using System.Security.Claims;
using ARPGServer.Contracts.Common;
using ARPGServer.Contracts.Saves;
using ARPGServer.Data;
using ARPGServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ARPGServer.Endpoints;

public static class SaveEndpoints
{
    private const int MaxSaveJsonLength = 512 * 1024;

    public static IEndpointRouteBuilder MapSaveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/save")
            .RequireAuthorization()
            .WithTags("Save");

        group.MapGet("/", GetSaveAsync)
            .WithName("GetCloudSave");

        group.MapPut("/", UpsertSaveAsync)
            .WithName("UpsertCloudSave");

        return app;
    }

    private static async Task<IResult> GetSaveAsync(ClaimsPrincipal principal, AppDbContext db)
    {
        var userId = principal.GetUserId();
        if (userId == null) return Results.Unauthorized();

        var save = await db.CloudSaves.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.UserId == userId.Value);

        if (save == null)
        {
            return Results.NotFound(new ErrorResponse("SAVE_NOT_FOUND", "Cloud save not found."));
        }

        return Results.Ok(new CloudSaveResponse(save.SaveJson, save.Version, save.UpdatedAtUtc));
    }

    private static async Task<IResult> UpsertSaveAsync(
        UpsertCloudSaveRequest request,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userId = principal.GetUserId();
        if (userId == null) return Results.Unauthorized();

        var saveJson = request.SaveJson ?? string.Empty;
        if (string.IsNullOrWhiteSpace(saveJson))
        {
            return Results.BadRequest(new ErrorResponse("EMPTY_SAVE", "SaveJson is required."));
        }

        if (saveJson.Length > MaxSaveJsonLength)
        {
            return Results.BadRequest(new ErrorResponse("SAVE_TOO_LARGE", "SaveJson is too large."));
        }

        var save = await db.CloudSaves.SingleOrDefaultAsync(entity => entity.UserId == userId.Value);
        if (save == null)
        {
            save = new CloudSave
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                SaveJson = saveJson,
                Version = 1,
                UpdatedAtUtc = DateTime.UtcNow
            };

            db.CloudSaves.Add(save);
        }
        else
        {
            save.SaveJson = saveJson;
            save.Version += 1;
            save.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new CloudSaveResponse(save.SaveJson, save.Version, save.UpdatedAtUtc));
    }
}
