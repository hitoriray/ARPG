namespace ARPGServer.Models;

public sealed class CloudSave
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string SaveJson { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public User? User { get; set; }
}
