using Microsoft.Extensions.Configuration;
using WpfApp = System.Windows.Application;
using WpfStartup = System.Windows.StartupEventArgs;

namespace RemoteServer;

public partial class App : WpfApp
{
    public static IConfiguration Configuration { get; private set; } = null!;

    protected override void OnStartup(WpfStartup e)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        base.OnStartup(e);
    }
}
