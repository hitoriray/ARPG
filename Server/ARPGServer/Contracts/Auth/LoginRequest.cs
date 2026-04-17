namespace ARPGServer.Contracts.Auth;

public sealed record LoginRequest(string? UserName, string? Password);
