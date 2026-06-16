using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using RemoteServer.Services;

namespace RemoteServer.ViewModels;

public class ServerViewModel : INotifyPropertyChanged
{
    private readonly SignalRClient _signalR;
    private readonly ScreenCaptureService _screenCapture;
    private readonly InputSimulatorService _inputSim;
    private readonly FileTransferService _fileTransfer;
    private readonly ChatService _chat;

    private string _connectionCode = "------";
    private string _status = "Desconectado";
    private bool _isRunning;
    private string _sessionName;
    private string _masterPassword;

    public string ConnectionCode { get => _connectionCode; set { _connectionCode = value; OnPropertyChanged(); } }
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    public bool IsRunning { get => _isRunning; set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStart)); } }
    public bool CanStart => !IsRunning;
    public ChatService ChatService => _chat;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ServerViewModel()
    {
        _signalR = new SignalRClient();
        _screenCapture = new ScreenCaptureService();
        _inputSim = new InputSimulatorService();
        _fileTransfer = new FileTransferService(_signalR);
        _chat = new ChatService(_signalR);

        _sessionName = App.Configuration["SessionName"] ?? "Mi PC";
        _masterPassword = App.Configuration["MasterPassword"] ?? "";

        WireEvents();
    }

    private void WireEvents()
    {
        _signalR.CodeGenerated += code =>
        {
            ConnectionCode = code;
            Status = $"Conectado - Código: {code}";
            _fileTransfer.SetSessionCode(code);
            _chat.SetSessionCode(code);
        };

        _signalR.SessionReady += () =>
        {
            Status = "¡Cliente conectado!";
            _screenCapture.Start();
        };

        _signalR.InputReceived += input =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (input.Type)
                {
                    case "MouseMove": _inputSim.MouseMove(input.X, input.Y); break;
                    case "MouseDown": _inputSim.MouseDown(input.Button); break;
                    case "MouseUp": _inputSim.MouseUp(input.Button); break;
                    case "MouseWheel": _inputSim.MouseWheel(input.Delta); break;
                    case "KeyDown": _inputSim.KeyDown(input.KeyCode); break;
                    case "KeyUp": _inputSim.KeyUp(input.KeyCode); break;
                }
            });
        };

        _signalR.PeerDisconnected += () =>
        {
            Status = "Cliente desconectado";
            _screenCapture.Stop();
        };

        _screenCapture.FrameCaptured += async frameData =>
        {
            if (!_signalR.IsConnected) return;
            try
            {
                await _signalR.SendFrame(ConnectionCode, frameData);
            }
            catch (Exception ex)
            {
                Status = $"Error enviando frame: {ex.Message}";
            }
        };
    }

    public async Task StartAsync()
    {
        IsRunning = true;
        Status = "Conectando al broker...";

        try
        {
            await _signalR.ConnectAsync();
            var passwordHash = ComputeHash(_masterPassword);
            await _signalR.RegisterServer(_sessionName, passwordHash);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            IsRunning = false;
        }
    }

    public async Task StopAsync()
    {
        _screenCapture.Stop();
        await _signalR.DisconnectAsync();
        IsRunning = false;
        ConnectionCode = "------";
        Status = "Desconectado";
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
