namespace ARPGServer.Contracts.Saves;

public sealed record UpsertCloudSaveRequest(string? SaveJson, int? ExpectedVersion);
