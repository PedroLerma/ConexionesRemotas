namespace RemoteClient.Services;

public class ChatService
{
    private readonly SignalRClient _signalR;
    private string? _sessionCode;

    public event Action<string, string, DateTime>? MessageReceived;

    public ChatService(SignalRClient signalR)
    {
        _signalR = signalR;
        _signalR.ChatReceived += OnChatReceived;
    }

    public void SetSessionCode(string code) => _sessionCode = code;

    private void OnChatReceived(string message, string sender, DateTime timestamp)
    {
        MessageReceived?.Invoke(message, sender, timestamp);
    }

    public async Task SendMessageAsync(string message)
    {
        if (_sessionCode == null) return;
        await _signalR.SendChat(_sessionCode, message);
    }
}
