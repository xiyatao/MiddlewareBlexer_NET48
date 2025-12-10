using Kinect_Middleware.Kinect;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Kinect_Middleware.Views.Pages {
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl {
        private UniversalKinect universalKinect;

        public MainView() {
            InitializeComponent();
            universalKinect = App.Host.Services.GetRequiredService<UniversalKinect>();

            // Set initial selection for ComboBox
            ComboBox.SelectedIndex = 0;

            DataContext = universalKinect.Bindings;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            String selected = ((ComboBoxItem)ComboBox.SelectedItem).Content.ToString();

            if (selected == "Azure Kinect") {
                universalKinect.selectedKinect = KinectType.Azure;
            } else if (selected == "Xbox one Kinect") {
                universalKinect.selectedKinect = KinectType.One;
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            universalKinect.start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e) {
            universalKinect.stop();
        }
    }
}
