using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace RemoteClient.ViewModels;

public class RemoteDesktopViewModel : INotifyPropertyChanged
{
    private BitmapImage? _screenImage;
    private string _status = "Conectado";

    public BitmapImage? ScreenImage { get => _screenImage; set { _screenImage = value; OnPropertyChanged(); } }
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
