using System.Security.Claims;
using System.Text.RegularExpressions;
using ARPGServer.Contracts.Auth;
using ARPGServer.Contracts.Common;
using ARPGServer.Data;
using ARPGServer.Models;
using ARPGServer.Security;
using Microsoft.EntityFrameworkCore;

namespace ARPGServer.Endpoints;

public static class AuthEndpoints
{
    private static readonly Regex UserNameRegex = new("^[a-z0-9_]{3,32}$", RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register");

        group.MapPost("/login", LoginAsync)
            .WithName("Login");

        group.MapGet("/me", GetMe)
            .RequireAuthorization()
            .WithName("GetMe");

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, AppDbContext db)
    {
        var userName = NormalizeUserName(request.UserName);
        var password = request.Password ?? string.Empty;

        var error = ValidateRegisterRequest(userName, password);
        if (error != null) return Results.BadRequest(error);

        var exists = await db.Users.AnyAsync(user => user.UserName == userName);
        if (exists)
        {
            return Results.Conflict(new ErrorResponse("USER_NAME_EXISTS", "User name already exists."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new ErrorResponse("USER_NAME_EXISTS", "User name already exists."));
        }

        return Results.Created(
            $"/api/users/{user.Id}",
            new RegisterResponse(user.Id, user.UserName, user.CreatedAtUtc));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        JwtTokenService jwtTokenService)
    {
        var userName = NormalizeUserName(request.UserName);
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            return Results.BadRequest(new ErrorResponse("INVALID_LOGIN_REQUEST", "User name and password are required."));
        }

        var user = await db.Users.SingleOrDefaultAsync(entity => entity.UserName == userName);
        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = jwtTokenService.CreateAccessToken(user);
        return Results.Ok(new LoginResponse(user.Id, user.UserName, token.Token, token.ExpiresAtUtc));
    }

    private static IResult GetMe(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        var userName = principal.FindFirstValue(ClaimTypes.Name);

        if (userId == null || string.IsNullOrWhiteSpace(userName))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new
        {
            userId = userId.Value,
            userName
        });
    }

    private static string NormalizeUserName(string? userName)
    {
        return (userName ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static ErrorResponse? ValidateRegisterRequest(string userName, string password)
    {
        if (!UserNameRegex.IsMatch(userName))
        {
            return new ErrorResponse(
                "INVALID_USER_NAME",
                "User name must be 3-32 chars and contain only lowercase letters, digits, or underscore.");
        }

        if (password.Length is < 8 or > 128)
        {
            return new ErrorResponse(
                "INVALID_PASSWORD",
                "Password length must be between 8 and 128.");
        }

        return null;
    }
}
