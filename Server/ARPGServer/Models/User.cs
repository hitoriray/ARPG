namespace ARPGServer.Models;

public sealed class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public CloudSave? CloudSave { get; set; }
}
