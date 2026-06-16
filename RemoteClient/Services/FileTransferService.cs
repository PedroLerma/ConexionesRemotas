using System.IO;

namespace RemoteClient.Services;

public class FileTransferService
{
    private readonly SignalRClient _signalR;
    private string? _sessionCode;
    private readonly Dictionary<string, FileStream> _incomingFiles = new();
    private readonly Dictionary<string, long> _incomingSizes = new();
    private readonly Dictionary<string, long> _receivedBytes = new();

    public event Action<string, string, long>? FileOfferReceived;
    public event Action<double>? TransferProgress;
    public event Action<string>? TransferComplete;

    public FileTransferService(SignalRClient signalR)
    {
        _signalR = signalR;
        _signalR.FileRequested += OnFileRequested;
        _signalR.FileChunkReceived += OnFileChunk;
    }

    public void SetSessionCode(string code) => _sessionCode = code;

    private void OnFileRequested(string fileId, string fileName, long fileSize)
    {
        FileOfferReceived?.Invoke(fileId, fileName, fileSize);
    }

    public async Task AcceptFileAsync(string fileId, string fileName, long fileSize, string savePath)
    {
        if (_sessionCode == null) return;
        var fullPath = Path.Combine(savePath, fileName);
        _incomingFiles[fileId] = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        _incomingSizes[fileId] = fileSize;
        _receivedBytes[fileId] = 0;
        await _signalR.AcceptFile(_sessionCode, fileId);
    }

    private void OnFileChunk(string fileId, byte[] data, int chunkIndex, bool isLast)
    {
        if (!_incomingFiles.TryGetValue(fileId, out var fs)) return;
        fs.Write(data, 0, data.Length);
        _receivedBytes[fileId] += data.Length;

        if (_incomingSizes.TryGetValue(fileId, out var totalSize) && totalSize > 0)
            TransferProgress?.Invoke((double)_receivedBytes[fileId] / totalSize * 100);

        if (isLast)
        {
            fs.Close();
            _incomingFiles.Remove(fileId);
            _incomingSizes.Remove(fileId);
            _receivedBytes.Remove(fileId);
            TransferComplete?.Invoke(fileId);
        }
    }

    public async Task SendFileAsync(string filePath)
    {
        if (_sessionCode == null) return;
        var fileInfo = new FileInfo(filePath);
        await _signalR.RequestFile(_sessionCode, fileInfo.Name, fileInfo.Length);
    }
}
