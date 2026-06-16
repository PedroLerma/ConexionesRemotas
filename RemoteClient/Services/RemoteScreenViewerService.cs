using System.IO;
using System.Windows.Media.Imaging;

namespace RemoteClient.Services;

public class RemoteScreenViewerService
{
    public event Action<BitmapImage>? FrameReady;

    public void ProcessFrame(byte[] jpegData)
    {
        try
        {
            using var ms = new MemoryStream(jpegData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            FrameReady?.Invoke(bitmap);
        }
        catch (Exception ex)
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "RemoteClient.log"), $"[{DateTime.Now:HH:mm:ss}] ProcessFrame error: {ex.Message}\n");
        }
    }
}
