using System.Windows;
using RemoteServer.ViewModels;

namespace RemoteServer.Views;

public partial class MainWindow : Window
{
    private readonly ServerViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new ServerViewModel();
        DataContext = _viewModel;
        ChatControlView.Initialize(_viewModel.ChatService);
    }

    private async void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.StartAsync();
    }

    private async void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.StopAsync();
    }

    private void SendFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            // TODO: implement file send via FileTransferService
        }
    }
}
