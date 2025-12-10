using Kinect_Middleware.Kinect;
using Kinect_Middleware.Scripts;
using Kinect_Middleware.Scripts.Web;
using Kinect_Middleware.UDP;
using Kinect_Middleware.Views.Pages; // keeping this extra using from Code 2
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Kinect_Middleware
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost Host { get; private set; }

        public App()
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                        .ConfigureServices((hostContext, services) =>
                        {
                            // For showing main view
                            services.AddSingleton<MainWindow>();

                            // For managing data that are exchange between Middleware and Unity
                            services.AddSingleton<UserResultsManager>();
                            services.AddSingleton<UserConfigurationManager>();

                            // For exchanging data with Unity
                            services.AddSingleton<UDPSend>();
                            services.AddSingleton<UDPReceive>();

                            // For storing settings
                            services.AddSingleton(AzureKinectPreferences.Read());
                            services.AddSingleton(AppSettings.Read());

                            // For receiving data from kinects
                            services.AddSingleton<UniversalKinect>();
                            services.AddSingleton<TCPReceiver>(); // added from Code 2
                        })
                        .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host.StartAsync();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Host.Services.GetRequiredService<AppSettings>().SwitchLanguage(this.Resources);

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Send data to server
            var userManager = Host.Services.GetRequiredService<UserConfigurationManager>();
            var userResultsManager = Host.Services.GetRequiredService<UserResultsManager>();
            userResultsManager.SendUnsubmittedResults(userManager.Username);

            // Finish closing the application
            await Host.StopAsync();
            base.OnExit(e);
        }
    }
}
