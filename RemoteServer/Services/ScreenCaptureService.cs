using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RemoteServer.Services;

public class ScreenCaptureService : IDisposable
{
    private System.Threading.Timer? _timer;
    private bool _running;
    private int _fps;
    private int _quality;

    public event Action<byte[]>? FrameCaptured;

    public ScreenCaptureService()
    {
        var config = App.Configuration.GetSection("ScreenCapture");
        _fps = int.Parse(config["Fps"] ?? "10");
        _quality = int.Parse(config["JpegQuality"] ?? "70");
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        var interval = 1000 / _fps;
        _timer = new System.Threading.Timer(_ => CaptureAndSend(), null, 0, interval);
    }

    public void Stop()
    {
        _running = false;
        _timer?.Dispose();
        _timer = null;
    }

    private void CaptureAndSend()
    {
        try
        {
            var left = (int)System.Windows.SystemParameters.VirtualScreenLeft;
            var top = (int)System.Windows.SystemParameters.VirtualScreenTop;
            var width = (int)System.Windows.SystemParameters.VirtualScreenWidth;
            var height = (int)System.Windows.SystemParameters.VirtualScreenHeight;

            using var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(left, top, 0, 0, bitmap.Size);

            using var ms = new MemoryStream();
            var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, _quality);
            bitmap.Save(ms, encoder, parameters);

            FrameCaptured?.Invoke(ms.ToArray());
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
