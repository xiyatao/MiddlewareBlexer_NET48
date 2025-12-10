using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// Class for simple display of messages
    /// </summary>
    internal class ShowMessage {
        public string Label;
        public ShowMessage(string label) {
            Label = label;
        }

        private static void showMessage(string label, string message, MessageBoxImage image) {
            AppSettings appSetting = App.Host.Services.GetService<AppSettings>();

            MessageBox.Show(
                message,
                label,
                MessageBoxButton.OK,
                image
            );
        }

        // Information

        public void Information(string error) {
            Information(Label, error);
        }

        public static void Information(string label, string error) {
            showMessage(label, error, MessageBoxImage.Information);
        }

        // Warning

        public void Warning(string error) {
            Warning(Label, error);
        }

        public void Warning(string label, string error) {
            showMessage(label, error, MessageBoxImage.Warning);
        }

        // Error

        public void Error(string error) {
            Error(Label, error);
        }

        public static void Error(string lable, string error) {
            showMessage(lable, error, MessageBoxImage.Error);
        }
    }
}
