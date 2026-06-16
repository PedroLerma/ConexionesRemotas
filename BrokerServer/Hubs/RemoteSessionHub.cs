using Microsoft.AspNetCore.SignalR;
using BrokerServer.Models;
using SharedLib;

namespace BrokerServer.Hubs;

public class RemoteSessionHub : Hub
{
    private readonly SessionManager _sessionManager;

    public RemoteSessionHub(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<SessionResult> RegisterServer(string sessionName, string passwordHash)
    {
        var session = _sessionManager.CreateSession(Context.ConnectionId, sessionName, passwordHash);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{session.Code}");
        await Clients.Caller.SendAsync("CodeGenerated", session.Code);
        return new SessionResult { Success = true, Code = session.Code };
    }

    public async Task<SessionResult> JoinSession(string code, string passwordHash)
    {
        var session = _sessionManager.ValidateAndJoin(code, passwordHash, Context.ConnectionId);
        if (session == null)
            return new SessionResult { Success = false, ErrorMessage = "Código o contraseña incorrectos" };

        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{code}");
        await Clients.Group($"session_{code}").SendAsync("SessionReady");
        return new SessionResult { Success = true };
    }

    public async Task SendFrame(string sessionCode, byte[] frameData)
    {
        await Clients.OthersInGroup($"session_{sessionCode}").SendAsync("ReceiveFrame", frameData);
    }

    public async Task SendInput(string sessionCode, InputData input)
    {
        await Clients.OthersInGroup($"session_{sessionCode}").SendAsync("ReceiveInput", input);
    }

    public async Task SendChat(string sessionCode, string message)
    {
        var session = _sessionManager.GetByCode(sessionCode);
        if (session == null) return;
        var sender = Context.ConnectionId == session.ServerConnectionId ? "Servidor" : "Cliente";
        await Clients.Group($"session_{sessionCode}").SendAsync("ReceiveChat", message, sender, DateTime.UtcNow);
    }

    public async Task RequestFile(string sessionCode, string fileName, long fileSize)
    {
        var fileId = Guid.NewGuid().ToString("N")[..8];
        await Clients.OthersInGroup($"session_{sessionCode}").SendAsync("FileRequested", fileId, fileName, fileSize);
    }

    public async Task AcceptFile(string sessionCode, string fileId)
    {
        await Clients.OthersInGroup($"session_{sessionCode}").SendAsync("FileAccepted", fileId);
    }

    public async Task RejectFile(string sessionCode, string fileId)
    {
        await Clients.OthersInGroup($"session_{sessionCode}").SendAsync("FileRejected", fileId);
    }

    public async Task SendFileChunk(string sessionCode, string fileId, byte[] data, int chunkIndex, bool isLast)
    {
        await Clients.OthersInGroup($"session_{sessionCode}").SendAsync("ReceiveFileChunk", fileId, data, chunkIndex, isLast);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var session = _sessionManager.GetByConnectionId(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Group($"session_{session.Code}").SendAsync("PeerDisconnected");
            _sessionManager.RemoveByCode(session.Code);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
