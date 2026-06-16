using System.Windows;
using System.Windows.Input;
using SharedLib;
using RemoteClient.Services;

namespace RemoteClient.Views;

public partial class RemoteDesktopWindow : Window
{
    private readonly SignalRClient _signalR;
    private readonly string _sessionCode;
    private readonly RemoteScreenViewerService _screenViewer;
    private readonly InputCaptureService _inputCapture;
    private readonly ChatService _chatService;
    private readonly FileTransferService _fileTransfer;
    private bool _isFullScreen = true;

    public RemoteDesktopWindow(SignalRClient signalR, string sessionCode)
    {
        InitializeComponent();

        _signalR = signalR;
        _sessionCode = sessionCode;

        _screenViewer = new RemoteScreenViewerService();
        _inputCapture = new InputCaptureService();
        _chatService = new ChatService(signalR);
        _fileTransfer = new FileTransferService(signalR);

        _chatService.SetSessionCode(sessionCode);
        _fileTransfer.SetSessionCode(sessionCode);

        _signalR.FrameReceived += data =>
        {
            Dispatcher.Invoke(() => _screenViewer.ProcessFrame(data));
        };

        _screenViewer.FrameReady += bitmap =>
        {
            Dispatcher.Invoke(() => ScreenImage.Source = bitmap);
        };

        _chatService.MessageReceived += (msg, sender, dt) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChatList.Items.Add(new { Message = msg, Sender = sender, Time = dt.ToLocalTime().ToString("HH:mm") });
            });
        };

        Loaded += (_, _) =>
        {
            _inputCapture.StartCapture(signalR, sessionCode);
        };

        MouseMove += (_, e) =>
        {
            var pos = e.GetPosition(this);
            TopBar.Visibility = pos.Y < 10 ? Visibility.Visible : Visibility.Collapsed;
        };
    }

    private void ToggleChat_Click(object sender, RoutedEventArgs e)
    {
        SidePanel.Width = SidePanel.Width.Value == 0 ? new System.Windows.GridLength(250) : new System.Windows.GridLength(0);
    }

    private async void ChatSend_Click(object sender, RoutedEventArgs e)
    {
        var text = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        await _chatService.SendMessageAsync(text);
        ChatInput.Clear();
    }

    private void ScreenImage_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(ScreenImage);
        var screenW = System.Windows.SystemParameters.VirtualScreenWidth;
        var screenH = System.Windows.SystemParameters.VirtualScreenHeight;
        var input = new InputData
        {
            Type = "MouseMove",
            X = (int)(pos.X * (screenW / ScreenImage.ActualWidth)),
            Y = (int)(pos.Y * (screenH / ScreenImage.ActualHeight))
        };
        _ = _signalR.SendInput(_sessionCode, input);
    }

    private void ScreenImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        int btn = e.ChangedButton switch
        {
            MouseButton.Left => 0,
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0
        };
        _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseDown", Button = btn });
    }

    private void ScreenImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        int btn = e.ChangedButton switch
        {
            MouseButton.Left => 0,
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0
        };
        _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseUp", Button = btn });
    }

    private void ScreenImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _ = _signalR.SendInput(_sessionCode, new InputData { Type = "MouseWheel", Delta = e.Delta });
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _isFullScreen = !_isFullScreen;
            WindowState = _isFullScreen ? WindowState.Maximized : WindowState.Normal;
            WindowStyle = _isFullScreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        _inputCapture.StopCapture();
        await _signalR.DisconnectAsync();
        Application.Current.Shutdown();
    }

    private void SendFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            _ = _fileTransfer.SendFileAsync(dialog.FileName);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _inputCapture.Dispose();
        base.OnClosed(e);
    }
}
