using System.Collections.Concurrent;
using BrokerServer.Services;

namespace BrokerServer.Models;

public class SessionManager
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
    private readonly CodeGenerator _codeGenerator;

    public SessionManager(CodeGenerator codeGenerator)
    {
        _codeGenerator = codeGenerator;
    }

    public SessionInfo CreateSession(string connectionId, string sessionName, string passwordHash)
    {
        var code = _codeGenerator.Generate();
        var session = new SessionInfo
        {
            Code = code,
            PasswordHash = passwordHash,
            ServerConnectionId = connectionId,
            SessionName = sessionName,
            CreatedAt = DateTime.UtcNow
        };
        _sessions[code] = session;
        CleanupExpired();
        return session;
    }

    public SessionInfo? ValidateAndJoin(string code, string passwordHash, string clientConnectionId)
    {
        if (!_sessions.TryGetValue(code, out var session))
            return null;
        if (session.PasswordHash != passwordHash)
            return null;
        if (session.ClientConnectionId != null)
            return null;

        session.ClientConnectionId = clientConnectionId;
        return session;
    }

    public SessionInfo? GetByCode(string code)
    {
        _sessions.TryGetValue(code, out var session);
        return session;
    }

    public SessionInfo? GetByConnectionId(string connectionId)
    {
        return _sessions.Values.FirstOrDefault(s => s.ServerConnectionId == connectionId || s.ClientConnectionId == connectionId);
    }

    public void RemoveByCode(string code)
    {
        _sessions.TryRemove(code, out _);
    }

    public void RemoveByConnectionId(string connectionId)
    {
        var session = GetByConnectionId(connectionId);
        if (session != null)
            _sessions.TryRemove(session.Code, out _);
    }

    private void CleanupExpired()
    {
        var expired = _sessions.Values
            .Where(s => DateTime.UtcNow - s.CreatedAt > TimeSpan.FromMinutes(10))
            .ToList();
        foreach (var s in expired)
            _sessions.TryRemove(s.Code, out _);
    }
}
