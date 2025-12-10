/*
    ===================================================================================
    Author          : Marilena Tsami
    Institution     : Universidad Politécnica de Madrid (UPM)
    Course          : Master in Internet of Things (IoT) – 2024/2025
    Modification    : Integrated centralized support for real-time facial emotion 
                      recognition within the UniversalKinect framework.
                      - Defined EmotionArrivedEventArgs for emotion data transport
                      - Routed emotion events from OneKinect and AzureKinect to UI
                      - Emitted bitmap frames containing emotion overlays
    Last Modified   : 2025-05-29
    ===================================================================================
*/

using Kinect_Middleware.Models;
using Kinect_Middleware.Scripts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kinect_Middleware.Kinect
{

    /// <summary>
    /// EventArgs subclass to carry joint (skeleton) data from Kinect
    /// </summary>
    public sealed class FrameArrivedEventArgs : EventArgs
    {
        public IDictionary<string, MJoint> dictionary;

        public FrameArrivedEventArgs(Dictionary<string, MJoint> dictionary)
        {
            this.dictionary = dictionary;
        }
    }

    /// <summary>
    /// EventArgs subclass to carry emotion data from Kinect
    /// </summary>    
    public sealed class EmotionArrivedEventArgs : EventArgs
    {
        public IDictionary<string, string> dictionary;

        public EmotionArrivedEventArgs(string emotion)
        {
            dictionary = new Dictionary<string, string>
            {
                { "emotion", emotion }
            };
        }
    }

    /// <summary>
    /// EventArgs subclass to carry bitmap (image/skeleton visuals) from Kinect
    /// </summary>
    public sealed class BitmapArrivedEventArgs : EventArgs
    {
        public enum Type
        {
            Image,     // Raw image from camera
            Skeleton,  // Overlay image showing the tracked skeleton
            Emotion    // Overlay image with emotion detection 
        }

        public BitmapSource bitmap;  // The actual image frame
        public Type type;            // What kind of image it is

        public BitmapArrivedEventArgs(BitmapSource bitmap, Type type)
        {
            this.bitmap = bitmap;
            this.type = type;
        }
    }

    /// <summary>
    /// Central class for managing both Kinect One and Azure Kinect devices.
    /// Also dispatches image and skeleton frame events to the rest of the app.
    /// </summary>
    public class UniversalKinect
    {
        // Kinect instances
        public OneKinect oneKinect;
        public AzureKinect azureKinect;

        // Currently selected Kinect type
        public KinectType selectedKinect = KinectType.None;

        // Events for sending Kinect data to listeners (like Views or processors)
        public EventHandler<FrameArrivedEventArgs> frameArrived;
        public EventHandler<EmotionArrivedEventArgs> emotionArrived;
        public EventHandler<BitmapArrivedEventArgs> bitmapArrived;

        // Background thread for polling frames
        Thread threadFrame;

        // The ViewModel that UI binds to (implements INotifyPropertyChanged)
        public UniversalKinectBindings Bindings = new UniversalKinectBindings();

        // Constructor
        public UniversalKinect() { }

        /// <summary>
        /// Starts the selected Kinect device, initializes it, and begins polling frames.
        /// </summary>
        public void start()
        {
            try
            {
                // Handle selection logic for which Kinect to activate
                switch (selectedKinect)
                {
                    case KinectType.None:
                        // No Kinect selected; ensure both are null
                        this.azureKinect = null;
                        this.oneKinect = null;
                        break;

                    case KinectType.One:
                        // Clean up Azure Kinect if it was running
                        if (azureKinect != null)
                        {
                            this.azureKinect.Dispose();
                            this.azureKinect = null;
                        }

                        // Initialize Kinect One
                        this.oneKinect = new OneKinect(this);
                        break;

                    case KinectType.Azure:
                        // Clean up Kinect One if it was running
                        if (oneKinect != null)
                        {
                            this.oneKinect.Dispose();
                            this.oneKinect = null;
                        }

                        // Initialize Azure Kinect
                        this.azureKinect = new AzureKinect(this);
                        break;
                }


            }
            catch (Exception err)
            {
                Console.WriteLine($"Error occurred: {err.Message}");
                Console.WriteLine("Connection error");
                // Handle exceptions gracefully (e.g., missing device)
                ShowMessage.Error(
                    "Connection error",
                    "Unable to connect with Kinect, check if device is connected properly or try changing settings"
                );
                return;
            }

            // Clean up old frame polling thread
            if (threadFrame != null)
            {
                threadFrame.Abort();  // ⚠ Deprecated, but used here for forced stop
                threadFrame = null;
            }

            // Start a new frame polling thread
            threadFrame = new Thread(new ThreadStart(getFrame));
            threadFrame.IsBackground = true; // Ends when the app ends
            threadFrame.Start();
        }

        /// <summary>
        /// Stops the active Kinect device and frame polling thread.
        /// </summary>
        public void stop()
        {
            // Offload stop logic to a background task to avoid blocking UI
            Task.Run(() =>
            {
                // Dispose Azure Kinect
                if (azureKinect != null)
                {
                    this.azureKinect.Dispose();
                    this.azureKinect = null;
                }

                // Dispose Kinect One
                if (oneKinect != null)
                {
                    this.oneKinect.Dispose();
                    this.oneKinect = null;
                }

                // Stop the frame polling thread
                if (threadFrame != null)
                {
                    threadFrame.Abort();  // ⚠ Not ideal; may cause issues in production
                    threadFrame = null;
                }
            });
        }

        /// <summary>
        /// Called repeatedly in a thread to get frames from the active Kinect.
        /// This method delegates to the active device’s getFrame() method.
        /// </summary>
        public void getFrame()
        {
            switch (selectedKinect)
            {
                case KinectType.None:
                    // Do nothing if no Kinect is selected
                    break;

                case KinectType.Azure:
                    // Start Azure Kinect's frame loop
                    this.azureKinect.getFrame();
                    break;

                case KinectType.One:
                    // Start Kinect One's frame loop
                    this.oneKinect.getFrame();
                    break;
            }
        }

        /// <summary>
        /// Assigns a unique color to each body tracked by Kinect.
        /// Used for multi-user skeleton visualization.
        /// </summary>
        public static SolidColorBrush BrushForBody(int index)
        {
            List<Color> bodyColor = new List<Color> {
                Colors.Red,
                Colors.Orange,
                Colors.Yellow,
                Colors.Green,
                Colors.Blue,
                Colors.Violet,
            };

            // Wrap around if more than 6 bodies (index % 6)
            return new SolidColorBrush(bodyColor[index % bodyColor.Count]);
        }
    }
}
