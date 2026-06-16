namespace BrokerServer.Models;

public class SessionInfo
{
    public string Code { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string ServerConnectionId { get; set; } = string.Empty;
    public string? ClientConnectionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive => ClientConnectionId == null;
    public bool IsComplete => ServerConnectionId != null && ClientConnectionId != null;
}
