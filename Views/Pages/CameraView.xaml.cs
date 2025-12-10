using Kinect_Middleware.Kinect;                         // Import Kinect handling logic
using Microsoft.Extensions.DependencyInjection;         // For dependency injection of services
using System.Windows;                                   // WPF base namespace
using System.Windows.Controls;                          // For UserControl and UI controls

namespace Kinect_Middleware.Views.Pages
{
    /// <summary>
    /// Code-behind for CameraView.xaml
    /// This view displays:
    /// - The color image from the Kinect
    /// - The skeleton overlay bitmap
    ///  - The emotion overlay bitmap 
    /// </summary>
    public partial class CameraView : UserControl
    {

        // Flag to make sure the event is only subscribed once
        private bool firstViewLoad = true;

        // Reference to the UniversalKinect instance (shared across app)
        private UniversalKinect universalKinect { get; set; }

        /// <summary>
        /// Constructor — called when the view is initialized.
        /// It gets the UniversalKinect service via dependency injection.
        /// </summary>
        public CameraView()
        {
            InitializeComponent(); // Load XAML content

            // Retrieve shared UniversalKinect instance from DI container (App.Host.Services)
            universalKinect = App.Host.Services.GetRequiredService<UniversalKinect>();
        }

        /// <summary>
        /// View_Loaded — called when the view is first loaded into memory/displayed.
        /// Sets up the event listener to receive bitmap updates (color and skeleton).
        /// </summary>
        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            // Only run once (the first time the view is loaded)
            if (firstViewLoad)
            {

                // Subscribe to the bitmapArrived event from the UniversalKinect
                universalKinect.bitmapArrived += (s, args) =>
                {

                    // Switch between image types: raw color or skeleton overlay
                    switch (args.type)
                    {

                        // Raw color image from the camera
                        case BitmapArrivedEventArgs.Type.Image:
                            bitmap.Dispatcher.Invoke(() =>
                            {
                                // Set the image source in the UI
                                bitmap.Source = args.bitmap;
                            });
                            break;

                        // Skeleton overlay image
                        case BitmapArrivedEventArgs.Type.Skeleton:
                            skeleton.Dispatcher.Invoke(() =>
                            {
                                // Set the overlay image source in the UI
                                skeleton.Source = args.bitmap;
                            });
                            break;

                        // Emotion overlay image - Marilena Tsami's FER model 2025
                        case BitmapArrivedEventArgs.Type.Emotion:
                            emotion.Dispatcher.Invoke(() =>
                            {
                                // Set the overlay image source in the UI
                                emotion.Source = args.bitmap;
                            });
                            break;
                    }
                };

                // Prevent this logic from running again on future loads
                firstViewLoad = false;
            }
        }
    }
}
