using Microsoft.Extensions.Configuration;

namespace RemoteClient;

public partial class App : System.Windows.Application
{
    public static IConfiguration Configuration { get; private set; } = null!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        base.OnStartup(e);
    }
}
