namespace ARPGServer.Contracts.Saves;

public sealed record CloudSaveResponse(
    string SaveJson,
    int Version,
    DateTime UpdatedAtUtc);
