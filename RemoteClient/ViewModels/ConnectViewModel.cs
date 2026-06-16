using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using RemoteClient.Services;
using RemoteClient.Views;

namespace RemoteClient.ViewModels;

public class ConnectViewModel : INotifyPropertyChanged
{
    private readonly SignalRClient _signalR;

    private string _code = "";
    private string _password = "";
    private string _status = "Ingresa el código y la contraseña";
    private bool _isConnecting;

    public string Code { get => _code; set { _code = value.ToUpper(); OnPropertyChanged(); OnPropertyChanged(nameof(CanConnect)); } }
    public string Password { get => _password; set { _password = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanConnect)); } }
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    public bool IsConnecting { get => _isConnecting; set { _isConnecting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanConnect)); } }
    public bool CanConnect => !IsConnecting && Code.Replace("-", "").Length >= 5 && Password.Length > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConnectViewModel()
    {
        _signalR = new SignalRClient();
    }

    public async Task ConnectAsync()
    {
        IsConnecting = true;
        Status = "Conectando al broker...";

        try
        {
            await _signalR.ConnectAsync();
            Status = "Uniéndose a la sesión...";

            var passwordHash = ComputeHash(Password);
            var result = await _signalR.JoinSession(Code, passwordHash);

            if (result.Success)
            {
                Status = "¡Conectado!";
                var desktopWindow = new RemoteDesktopWindow(_signalR, Code);
                desktopWindow.Show();

                foreach (Window win in Application.Current.Windows)
                {
                    if (win is Views.ConnectWindow)
                        win.Hide();
                }

                _signalR.PeerDisconnected += () =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("La conexión se perdió");
                        Application.Current.Shutdown();
                    });
                };
            }
            else
            {
                Status = $"Error: {result.ErrorMessage}";
                IsConnecting = false;
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            IsConnecting = false;
        }
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
