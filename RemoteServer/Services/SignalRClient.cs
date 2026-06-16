using Microsoft.AspNetCore.SignalR.Client;
using SharedLib;

namespace RemoteServer.Services;

public class SignalRClient
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public event Action<string>? CodeGenerated;
    public event Action? SessionReady;
    public event Action<string, string, DateTime>? ChatReceived;
    public event Action<InputData>? InputReceived;
    public event Action<string, string, long>? FileRequested;
    public event Action<string>? FileAccepted;
    public event Action<string>? FileRejected;
    public event Action<string, byte[], int, bool>? FileChunkReceived;
    public event Action? PeerDisconnected;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRClient()
    {
        var brokerUrl = App.Configuration["BrokerUrl"]!;
        _hubUrl = $"{brokerUrl.TrimEnd('/')}/hub";
    }

    public async Task ConnectAsync()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string>("CodeGenerated", code => CodeGenerated?.Invoke(code));
        _connection.On("SessionReady", () => SessionReady?.Invoke());
        _connection.On<string, string, DateTime>("ReceiveChat", (msg, sender, dt) => ChatReceived?.Invoke(msg, sender, dt));
        _connection.On<InputData>("ReceiveInput", input => InputReceived?.Invoke(input));
        _connection.On<string, string, long>("FileRequested", (id, name, size) => FileRequested?.Invoke(id, name, size));
        _connection.On<string>("FileAccepted", id => FileAccepted?.Invoke(id));
        _connection.On<string>("FileRejected", id => FileRejected?.Invoke(id));
        _connection.On<string, byte[], int, bool>("ReceiveFileChunk", (id, data, idx, last) => FileChunkReceived?.Invoke(id, data, idx, last));
        _connection.On("PeerDisconnected", () => PeerDisconnected?.Invoke());

        await _connection.StartAsync();
    }

    public async Task<SessionResult> RegisterServer(string sessionName, string passwordHash)
    {
        return await _connection!.InvokeAsync<SessionResult>("RegisterServer", sessionName, passwordHash);
    }

    public async Task SendFrame(string sessionCode, byte[] frameData)
    {
        await _connection!.InvokeAsync("SendFrame", sessionCode, frameData);
    }

    public async Task SendChat(string sessionCode, string message)
    {
        await _connection!.InvokeAsync("SendChat", sessionCode, message);
    }

    public async Task RequestFile(string sessionCode, string fileName, long fileSize)
    {
        await _connection!.InvokeAsync("RequestFile", sessionCode, fileName, fileSize);
    }

    public async Task AcceptFile(string sessionCode, string fileId)
    {
        await _connection!.InvokeAsync("AcceptFile", sessionCode, fileId);
    }

    public async Task RejectFile(string sessionCode, string fileId)
    {
        await _connection!.InvokeAsync("RejectFile", sessionCode, fileId);
    }

    public async Task SendFileChunk(string sessionCode, string fileId, byte[] data, int chunkIndex, bool isLast)
    {
        await _connection!.InvokeAsync("SendFileChunk", sessionCode, fileId, data, chunkIndex, isLast);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
            await _connection.StopAsync();
    }
}
