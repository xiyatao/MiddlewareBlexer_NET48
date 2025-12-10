using Kinect_Middleware.Kinect;
using Kinect_Middleware.Scripts;
using Kinect_Middleware.Views.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Kinect_Middleware.Views.Pages {
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl {
        private AzureKinectPreferences azurePreferences { get; set; }
        private AppSettings appSettings { get; set; }

        public SettingsView() {
            InitializeComponent();

            appSettings = App.Host.Services.GetService<AppSettings>();
            azurePreferences = App.Host.Services.GetService<AzureKinectPreferences>();

            setUpComboBox(
                LanguageSelector,
                appSettings.Language
            );
            setUpComboBox(
                SensorOrientationSelector,
                azurePreferences.Orientation
            );
            setUpComboBox(
                ProcessingModeSelector,
                azurePreferences.ProcessingMode
            );
            setUpComboBox(
                ColorResolutionSelector,
                azurePreferences.Resolution
            );
            setUpComboBox(
                FPSSelector,
                azurePreferences.CameraFPS
            );
        }

        private void setUpComboBox<T>(Selector selector, SettingsOption<T> settings) {
            selector.ItemsSource = settings.Options;
            selector.SelectedIndex = settings.Index;
        }

        private void Selector_SelectionChanged(object sender, SelectorChangedEventArgs e) {
            Selector comboBox = sender as Selector;
            int index = e.SelectedIndex;

            switch (comboBox.Name) {
                case "LanguageSelector":
                    appSettings.Language.Index = index;
                    appSettings.Save();
                    break;
                case "SensorOrientationSelector":
                    azurePreferences.Orientation.Index = index;
                    azurePreferences.Save();
                    break;
                case "ProcessingModeSelector":
                    azurePreferences.ProcessingMode.Index = index;
                    azurePreferences.Save();
                    break;
                case "ColorResolutionSelector":
                    azurePreferences.Resolution.Index = index;
                    azurePreferences.Save();
                    break;
                case "FPSSelector":
                    azurePreferences.CameraFPS.Index = index;
                    azurePreferences.Save();
                    break;
            }
        }
    }
}
