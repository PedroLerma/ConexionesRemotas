using System.IO;

namespace RemoteServer.Services;

public class FileTransferService
{
    private readonly SignalRClient _signalR;
    private string? _sessionCode;

    public event Action<double>? TransferProgress;
    public event Action<string>? TransferComplete;
    public event Action<string, string, long>? IncomingFileRequest;

    public FileTransferService(SignalRClient signalR)
    {
        _signalR = signalR;
        _signalR.FileRequested += OnFileRequested;
    }

    public void SetSessionCode(string code) => _sessionCode = code;

    private void OnFileRequested(string fileId, string fileName, long fileSize)
    {
        IncomingFileRequest?.Invoke(fileId, fileName, fileSize);
    }

    public async Task RequestFileAsync(string filePath)
    {
        if (_sessionCode == null) return;
        var fileInfo = new FileInfo(filePath);
        await _signalR.RequestFile(_sessionCode, fileInfo.Name, fileInfo.Length);
    }

    public async Task StartSendingAsync(string fileId, string filePath)
    {
        if (_sessionCode == null) return;
        const int chunkSize = 64 * 1024;
        var fileInfo = new FileInfo(filePath);
        long totalBytes = fileInfo.Length;
        long sentBytes = 0;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[chunkSize];
        int chunkIndex = 0;
        int bytesRead;

        while ((bytesRead = await fs.ReadAsync(buffer, 0, chunkSize)) > 0)
        {
            bool isLast = bytesRead < chunkSize;
            byte[] chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);

            await _signalR.SendFileChunk(_sessionCode, fileId, chunk, chunkIndex, isLast);
            sentBytes += bytesRead;
            TransferProgress?.Invoke((double)sentBytes / totalBytes * 100);
            chunkIndex++;
        }

        TransferComplete?.Invoke(fileInfo.Name);
    }
}
