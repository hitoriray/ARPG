namespace ARPGServer.Contracts.Auth;

public sealed record RegisterRequest(string? UserName, string? PhoneNumber, string? Password);
