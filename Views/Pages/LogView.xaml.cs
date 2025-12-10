using Kinect_Middleware.Kinect;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Kinect_Middleware.Views.Pages {
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogView : UserControl {
        private UniversalKinectBindings bindings;

        public LogView() {
            InitializeComponent();
            bindings = App.Host.Services.GetRequiredService<UniversalKinect>().Bindings;

            NameTextBox.DataContext = bindings;
        }
    }
}
