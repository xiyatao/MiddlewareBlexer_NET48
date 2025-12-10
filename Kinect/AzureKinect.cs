/*
    ===================================================================================
    Author          : Marilena Tsami
    Institution     : Universidad Politécnica de Madrid (UPM)
    Course          : Master in Internet of Things (IoT) – 2024/2025
    Modification    : Added real-time facial emotion recognition using a pre-trained
                      deep learning model integrated via EmguCV.
                      - Detects the most prominent face in the image captured by Azure Kinect
                      - Predicts emotion from the facial region of interest
                      - Overlays bounding box and emotion label on the live video
                      - Emits bitmap and emotion events for UI/logic integration
    Last Modified   : 2025-05-29
    ===================================================================================
*/

using Emgu.CV;
using Emgu.CV.Structure;
using EmotionDetector;
using Kinect_Middleware.Models;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pen = System.Windows.Media.Pen;
using Rectangle = System.Drawing.Rectangle;


namespace Kinect_Middleware.Kinect
{
    /// <summary>
    /// Class responsible for communicating with the Azure Kinect generates a 2D and 3D skeleton, and color image
    /// </summary>
    public class AzureKinect : IDisposable
    {
        UniversalKinect universalKinect;
        AzureKinectPreferences preferences;

        public Device device;
        private Tracker tracker;

        public bool _isAzuerRunning = false;
        public bool isAzuerRunning
        {
            get { return _isAzuerRunning; }
            set
            {
                _isAzuerRunning = value;
                universalKinect.Bindings.IsAzureKinectRunning = value;
            }
        }

        private FaceDetector faceDetector;
        private EmotionPredictor emotionPredictor;

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Initialization
        ///|////////////////////////////////////////////////////////////////////////////

        public AzureKinect(UniversalKinect universalKinect)
        {
            this.universalKinect = universalKinect;

            preferences = App.Host.Services.GetRequiredService<AzureKinectPreferences>();

            device = Device.Open();
            device.StartCameras(getDeviceConfig());

            Calibration calibration = device.GetCalibration();
            tracker = Tracker.Create(calibration, getTrackerConfig());

            isAzuerRunning = true;

            emotionPredictor = new EmotionPredictor();
            faceDetector = new FaceDetector();
        }


        ///|////////////////////////////////////////////////////////////////////////////
        //|| Dispose / Deinitialization
        ///|////////////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (device != null)
            {
                device.StopCameras();
            }
            if (tracker != null)
            {
                tracker.Shutdown();
            }

            if (disposing)
            {
                device.Dispose();
                tracker.Dispose();
            }

            universalKinect.Bindings.IsAzureKinectRunning = false;
        }

        ~AzureKinect()
        {
            Dispose(false);
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Logic
        ///|////////////////////////////////////////////////////////////////////////////

        public bool isAvailable()
        {
            return (Device.GetInstalledCount() > 0);
        }

        // Get Frame
        public void getFrame()
        {
            while (isAzuerRunning)
            {
                try
                {
                    using (Capture capture = device.GetCapture())
                    {
                        tracker.EnqueueCapture(capture);
                        Image color = capture.Color;

                        generateImage(color);
                        generateImageWithEmotionOverlay(color);

                        using (Frame frame = tracker.PopResult(TimeSpan.Zero, false))
                        {
                            if (frame != null)
                            {
                                generateSkeletonData(frame);

                                generateImageWithSkeletonOverlay(color, frame);
                            }
                        }

                    }
                }
                catch
                {
                    // Ignore error and continue
                }
            }
        }

        private void generateSkeletonData(Frame frame)
        {
            if (frame.NumberOfBodies >= 1)
            {
                Skeleton skeleton = frame.GetBodySkeleton(0);

                Dictionary<string, MJoint> dictionary = new Dictionary<string, MJoint>();

                for (int index = 0; index < Skeleton.JointCount; index++)
                {
                    JointId jointId = (JointId)index;
                    Joint joint = skeleton.GetJoint(index);

                    try
                    {
                        MJoint mJoint = new MJoint(jointId, joint.Position, joint.Quaternion);
                        dictionary.Add(mJoint.name, mJoint);
                    }
                    catch
                    {
                        // Ignore not implemented joints
                    }
                }

                if (universalKinect.frameArrived != null)
                {
                    universalKinect.frameArrived.Invoke(this, new FrameArrivedEventArgs(dictionary));
                }
            }
        }

        private void generateImage(Image color)
        {
            BitmapSource bitmap = BitmapSource.Create(
                                color.WidthPixels,
                                color.HeightPixels,
                                96,
                                96,
                                PixelFormats.Bgra32,
                                null,
                                color.Memory.ToArray(),
                                color.StrideBytes
                            );


            bitmap.Freeze();

            if (universalKinect.bitmapArrived != null)
            {
                universalKinect.bitmapArrived.Invoke(this, new BitmapArrivedEventArgs(
                    bitmap,
                    BitmapArrivedEventArgs.Type.Image
                ));
            }
        }

        /// <summary>
        /// Processes a BGRA image from the Azure Kinect, detects a face, runs emotion prediction,
        /// and overlays the result (bounding box + label) on the image. The final annotated bitmap
        /// is then emitted via the `bitmapArrived` event, and emotion data is broadcast via the
        /// `emotionArrived` event.
        /// </summary>
        /// <param name="azureColorImage">The Azure Kinect color image in BGRA32 format.</param>
        private void generateImageWithEmotionOverlay(Image azureColorImage)
        {
            // Validate the input image: must not be null and must be in BGRA32 format
            if (azureColorImage == null || azureColorImage.Format != ImageFormat.ColorBGRA32)
                return;

            string emotionLabel = null; // Placeholder for predicted emotion label

            // Extract image dimensions and stride from the Azure Kinect image
            int width = azureColorImage.WidthPixels;
            int height = azureColorImage.HeightPixels;
            int stride = azureColorImage.StrideBytes;
            int size = stride * height;

            // Copy image buffer from Azure Kinect image into a managed byte array
            byte[] pixels = azureColorImage.Memory.ToArray(); // Use the Memory property instead of Buffer

            // Create an EmguCV image (BGRA format) from the raw pixel buffer
            Image<Bgra, byte> image = new Image<Bgra, byte>(width, height);
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, image.Mat.DataPointer, pixels.Length);

            // Create a DrawingVisual for overlaying WPF drawing (rectangle, text) on top of the image
            DrawingVisual visual = new DrawingVisual();

            // Attempt to detect the best face in the current image
            var detectedFace = faceDetector.DetectBestFace(image); // Assumes your detector can handle EmguCV images

            // Open the drawing context for rendering overlays
            using (DrawingContext dc = visual.RenderOpen())
            {
                // If a face was found, process it
                if (detectedFace.HasValue)
                {
                    Rectangle face = detectedFace.Value;

                    // Configure drawing styles: transparent fill, lime green border
                    SolidColorBrush rectBrush = new SolidColorBrush(Colors.Transparent);
                    Pen rectPen = new Pen(new SolidColorBrush(Colors.Lime), 3);
                    rectPen.Freeze(); // Freeze for better WPF performance

                    // Draw the bounding box around the detected face
                    dc.DrawRectangle(rectBrush, rectPen, new Rect(face.X, face.Y, face.Width, face.Height));

                    // Create a System.Drawing.Rectangle from the detected face
                    var faceRect = new Rectangle(face.X, face.Y, face.Width, face.Height);

                    // Ensure the face rectangle is fully inside the image bounds
                    if (faceRect.Width > 0 && faceRect.Height > 0 &&
                        faceRect.Right <= image.Width && faceRect.Bottom <= image.Height)
                    {
                        try
                        {
                            // Crop the face region, convert to grayscale, resize to 72x72 for the predictor
                            var roi = image.Copy(faceRect)
                                           .Convert<Gray, byte>()
                                           .Resize(72, 72, Emgu.CV.CvEnum.Inter.Cubic);

                            // Run emotion prediction on the cropped face
                            emotionLabel = emotionPredictor.Predict(roi);

                            // If prediction was successful, draw the label above the face
                            if (!string.IsNullOrEmpty(emotionLabel))
                            {
                                // Configure WPF text rendering
                                FormattedText text = new FormattedText(
                                    emotionLabel,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    FlowDirection.LeftToRight,
                                    new Typeface("Segoe UI"),
                                    32,
                                    Brushes.Lime,
                                    VisualTreeHelper.GetDpi(visual).PixelsPerDip
                                );

                                // Position the label just above the face bounding box
                                Point textPos = new Point(face.X, Math.Max(face.Y - text.Height - 5, 0));
                                dc.DrawText(text, textPos);

                                // Fire the emotion event with the predicted label
                                universalKinect.emotionArrived?.Invoke(this, new EmotionArrivedEventArgs(emotionLabel));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Emotion ROI processing failed: " + ex.Message);
                        }
                    }
                }
            }

            // Create a WPF bitmap from the drawing visual (image + overlays)
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);
            renderBitmap.Freeze(); // Freeze to make it thread-safe and immutable

            // Fire the event to return the final image with overlay
            universalKinect.bitmapArrived?.Invoke(this, new BitmapArrivedEventArgs(renderBitmap, BitmapArrivedEventArgs.Type.Emotion));
        }

        private void generateImageWithSkeletonOverlay(Image color, Frame frame)
        {
            // Create a DrawingVisual object
            DrawingVisual drawingVisual = new DrawingVisual();

            Calibration cal = device.GetCalibration();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                //drawingContext.DrawImage(bitmap, new Rect(0, 0, color.WidthPixels, color.HeightPixels));

                for (uint skeletonIndex = 0; skeletonIndex < frame.NumberOfBodies; skeletonIndex++)
                {
                    Skeleton skeleton = frame.GetBodySkeleton(skeletonIndex);

                    // Body index is an index that is constant between frames for a given person
                    int bodyIndex = (int)frame.GetBodyId(skeletonIndex);
                    SolidColorBrush solidColorBrush = UniversalKinect.BrushForBody(bodyIndex);

                    for (int jointIndex = 0; jointIndex < Skeleton.JointCount; jointIndex++)
                    {
                        Joint joint = skeleton.GetJoint(jointIndex);
                        Vector2? vec = cal.TransformTo2D(joint.Position, CalibrationDeviceType.Depth, CalibrationDeviceType.Color);

                        drawingContext.DrawEllipse(solidColorBrush, null, new Point(vec.Value.X, vec.Value.Y), 10, 10);
                    }
                }
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(color.WidthPixels, color.HeightPixels, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            bmp.Freeze();

            if (universalKinect.bitmapArrived != null)
            {
                universalKinect.bitmapArrived.Invoke(this, new BitmapArrivedEventArgs(
                    bmp,
                    BitmapArrivedEventArgs.Type.Skeleton
                ));
            }
        }

        private DeviceConfiguration getDeviceConfig()
        {
            DeviceConfiguration config = new DeviceConfiguration();

            config.ColorFormat = ImageFormat.ColorBGRA32;
            config.ColorResolution = preferences.Resolution.Value;
            config.DepthMode = DepthMode.NFOV_Unbinned;
            config.SynchronizedImagesOnly = true;
            config.CameraFPS = preferences.CameraFPS.Value;
            config.WiredSyncMode = WiredSyncMode.Standalone;

            return config;
        }

        private TrackerConfiguration getTrackerConfig()
        {
            return new TrackerConfiguration
            {
                SensorOrientation = preferences.Orientation.Value,
                ProcessingMode = preferences.ProcessingMode.Value,
                GpuDeviceId = 0,
            };
        }
    }
}
