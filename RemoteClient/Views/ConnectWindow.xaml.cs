using System.Windows;
using System.Windows.Controls;
using RemoteClient.ViewModels;

namespace RemoteClient.Views;

public partial class ConnectWindow : Window
{
    private readonly ConnectViewModel _viewModel;

    public ConnectWindow()
    {
        InitializeComponent();
        _viewModel = new ConnectViewModel();
        DataContext = _viewModel;
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ConnectAsync();
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordInput.Password;
    }
}
