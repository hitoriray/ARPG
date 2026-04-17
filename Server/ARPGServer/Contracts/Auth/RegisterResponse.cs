namespace ARPGServer.Contracts.Auth;

public sealed record RegisterResponse(Guid UserId, string UserName, DateTime CreatedAtUtc);
