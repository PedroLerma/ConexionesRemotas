using System.Windows;
using System.Windows.Controls;
using RemoteServer.Services;

namespace RemoteServer.Views;

public partial class ChatControl : System.Windows.Controls.UserControl
{
    public ChatService? ChatService { get; set; }

    public ChatControl()
    {
        InitializeComponent();
    }

    public void Initialize(ChatService chatService)
    {
        ChatService = chatService;
        ChatService.MessageReceived += (msg, sender, dt) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChatList.Items.Add(new { Message = msg, Sender = sender, Timestamp = dt.ToLocalTime().ToString("HH:mm") });
            });
        };

        ChatService.MessageSent += (msg) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChatList.Items.Add(new { Message = msg, Sender = "Tú", Timestamp = DateTime.Now.ToString("HH:mm") });
            });
        };
    }

    private void SendChat_Click(object sender, RoutedEventArgs e)
    {
        var text = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text) || ChatService == null) return;
        _ = ChatService.SendMessageAsync(text);
        ChatInput.Clear();
    }
}
