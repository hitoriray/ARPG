namespace ARPGServer.Contracts.Auth;

public sealed record LoginResponse(
    Guid UserId,
    string UserName,
    string AccessToken,
    DateTime ExpiresAtUtc);
