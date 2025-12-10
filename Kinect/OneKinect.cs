/*
    ===================================================================================
    Author          : Marilena Tsami
    Institution     : Universidad Politécnica de Madrid (UPM)
    Course          : Master in Internet of Things (IoT) – 2024/2025
    Modification    : Added real-time facial emotion recognition using a pre-trained
                      deep learning model integrated via EmguCV.
                      - Detects the most prominent face in the image captured by Kinect One
                      - Predicts emotion from the facial region of interest
                      - Overlays bounding box and emotion label on the live video
                      - Emits bitmap and emotion events for UI/logic integration
    Last Modified   : 2025-05-29
    ===================================================================================
*/


using Emgu.CV;
using Emgu.CV.Structure;
using EmotionDetector;
using Kinect_Middleware.Models;             // Contains MJoint class representing a skeleton joint
using Microsoft.Kinect;                     // Kinect for Windows SDK
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;                      // For 3D math operations (Vector3, Quaternion)
using System.Windows;
using System.Windows.Media;                 // Drawing visuals
using System.Windows.Media.Imaging;         // Image handling for WPF
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;         // For rectangle handling in EmguCV

namespace Kinect_Middleware.Kinect
{

    /// <summary>
    /// Handles communication with Kinect One device.
    /// Responsible for acquiring:
    /// - Color images
    /// - Skeleton tracking data (2D overlay + 3D coordinates)
    /// </summary>
    public class OneKinect : IDisposable
    {
        UniversalKinect universalKinect;  // Parent manager that dispatches Kinect data to the app

        KinectSensor kinectSensor;        // Kinect hardware interface

        CoordinateMapper coordinateMapper; // Used to convert 3D joint positions to 2D image positions
        Body[] bodies;                    // Array of tracked bodies in a single frame

        MultiSourceFrameReader multiSourceFrameReader; // Reads both color and body frames

        private FaceDetector faceDetector;
        private EmotionPredictor emotionPredictor;

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Initialization
        ///|////////////////////////////////////////////////////////////////////////////

        public OneKinect(UniversalKinect universalKinect)
        {
            this.universalKinect = universalKinect;

            // Get the default Kinect sensor connected to the machine
            kinectSensor = KinectSensor.GetDefault();

            // Get a coordinate mapper to convert joint positions to image space
            coordinateMapper = kinectSensor.CoordinateMapper;

            // Read both color and body (skeleton) streams
            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(
                FrameSourceTypes.Color | FrameSourceTypes.Body
            );

            // Start the Kinect sensor
            kinectSensor.Open();

            // Update binding to notify UI that Kinect One is running
            universalKinect.Bindings.IsKinectOneRunning = true;

            emotionPredictor = new EmotionPredictor();
            faceDetector = new FaceDetector();
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Dispose / Deinitialization
        ///|////////////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from being called
        }

        protected virtual void Dispose(bool disposing)
        {
            // Unsubscribe from the event to prevent memory leaks
            multiSourceFrameReader.MultiSourceFrameArrived -= Reader_FrameArrived;

            // Pause frame reader before disposal
            multiSourceFrameReader.IsPaused = true;

            if (disposing)
            {
                multiSourceFrameReader.Dispose();
            }

            // Close the Kinect sensor
            if (kinectSensor != null)
            {
                kinectSensor.Close();
            }

            // Update bindings (UI)
            universalKinect.Bindings.IsKinectOneRunning = false;
        }

        ~OneKinect()
        {
            // Fallback cleanup in case Dispose wasn't called manually
            Dispose(false);
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Frame Acquisition Logic
        ///|////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Subscribes to the frame event so we receive new frames continuously
        /// </summary>
        public void getFrame()
        {
            multiSourceFrameReader.MultiSourceFrameArrived += Reader_FrameArrived;
        }

        /// <summary>
        /// Called every time a new frame arrives from the Kinect (color + body)
        /// </summary>
        private void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame == null) return;

            // Handle color image frame
            using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
            {
                generateImage(colorFrame);
                generateImageWithEmotionOverlay(colorFrame);

            }

            // Handle skeleton (body) frame
            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // Refresh current frame with new body data
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                }
            }

            // Generate visual overlay with skeleton
            generateImageWithSkeletonOverlay(bodies);

            // Send only the first tracked skeleton (for simplicity)
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];
                if (body.IsTracked)
                {
                    generateSkeletonData(body);
                    break;
                }
            }
        }

        /// <summary>
        /// Converts 3D Kinect skeleton data to a dictionary of MJoint objects.
        /// Sends data through frameArrived event.
        /// </summary>
        private void generateSkeletonData(Body body)
        {
            Dictionary<string, MJoint> skeleton = new Dictionary<string, MJoint>();

            foreach (JointType jointType in body.Joints.Keys)
            {
                // Read joint orientation and convert to System.Numerics.Quaternion
                Microsoft.Kinect.Vector4 orientation = body.JointOrientations[jointType].Orientation;
                Quaternion quaternion = new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W);

                // Read joint position and convert to System.Numerics.Vector3
                CameraSpacePoint point = body.Joints[jointType].Position;
                Vector3 position = new Vector3(point.X, point.Y, point.Z);

                try
                {
                    // Create MJoint and add it to skeleton dictionary
                    MJoint mJoint = new MJoint(jointType.ToString(), position, quaternion);
                    skeleton.Add(mJoint.name, mJoint);
                }
                catch
                {
                    // Silently skip joints that are unsupported or invalid
                }
            }

            // Fire skeleton event
            universalKinect.frameArrived?.Invoke(this, new FrameArrivedEventArgs(skeleton));
        }

        /// <summary>
        /// Generates a bitmap image from the Kinect’s color frame and sends it via bitmapArrived.
        /// </summary>
        private void generateImage(ColorFrame colorFrame)
        {
            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                int width = colorFrameDescription.Width;
                int height = colorFrameDescription.Height;

                // Prepare WPF WriteableBitmap to store pixel data
                WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

                // Copy pixel buffer from Kinect to bitmap
                colorFrame.CopyConvertedFrameDataToIntPtr(
                    bitmap.BackBuffer,
                    (uint)(width * height * 4),
                    ColorImageFormat.Bgra
                );

                // Finalize and freeze image for WPF thread safety
                bitmap.Lock();
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                bitmap.Unlock();
                bitmap.Freeze();

                // Fire image event
                universalKinect.bitmapArrived?.Invoke(this, new BitmapArrivedEventArgs(bitmap, BitmapArrivedEventArgs.Type.Image));
            }
        }


        /// <summary>
        /// Processes a color frame from the Kinect One, detects a face, runs emotion prediction,
        /// and overlays the result (bounding box + label) on the image. The final annotated bitmap
        /// is then emitted via the `bitmapArrived` event, and emotion data is broadcast via the
        /// `emotionArrived` event.        /// </summary>
        /// <param name="colorFrame">The Kinect ColorFrame containing the current video frame data.</param>
        private void generateImageWithEmotionOverlay(ColorFrame colorFrame)
        {
            // Return immediately if no color frame is available
            if (colorFrame == null)
                return;

            string emotionLabel = null; // Initialize emotion label to null

            // Get the frame dimensions
            int width = colorFrame.FrameDescription.Width;
            int height = colorFrame.FrameDescription.Height;

            // Convert ColorFrame to Image<Bgra, byte> (EmguCV)
            byte[] pixels = new byte[width * height * 4]; // BGRA format, 4 bytes per pixel
            colorFrame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);

            Image<Bgra, byte> image = new Image<Bgra, byte>(width, height);
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, image.Mat.DataPointer, pixels.Length);

            // Create a DrawingVisual object to enable drawing overlays on top of the bitmap
            DrawingVisual visual = new DrawingVisual();

            // Get the detected face bounding rectangle
            var detectedFace = faceDetector.DetectBestFace(image);

            // Open the DrawingContext for rendering drawing commands
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Check if we have a detected face rectangle to draw
                if (detectedFace.HasValue)
                {
                    Rectangle face = detectedFace.Value;

                    // Create a transparent brush to draw the rectangle border, no fill
                    SolidColorBrush rectBrush = new SolidColorBrush(Colors.Transparent);

                    // Create a lime-green pen for the rectangle border, 3 pixels thick
                    Pen rectPen = new Pen(new SolidColorBrush(Colors.Lime), 3);

                    // Freeze the pen to improve performance (immutable)
                    rectPen.Freeze();

                    // Draw the face bounding box rectangle on the image
                    // Convert System.Drawing.Rectangle to System.Windows.Rect for WPF drawing
                    dc.DrawRectangle(rectBrush, rectPen, new Rect(face.X, face.Y, face.Width, face.Height));

                    // Safety check for ROI boundaries inside the image
                    var faceRect = new Rectangle(face.X, face.Y, face.Width, face.Height);

                    if (faceRect.Width > 0 && faceRect.Height > 0 &&
                        faceRect.Right <= image.Width && faceRect.Bottom <= image.Height)
                    {
                        try
                        {
                            // Crop ROI, convert to grayscale, resize for emotion prediction
                            var roi = image.Copy(faceRect).Convert<Gray, byte>().Resize(72, 72, Emgu.CV.CvEnum.Inter.Cubic);

                            emotionLabel = emotionPredictor.Predict(roi);

                            if (!string.IsNullOrEmpty(emotionLabel))
                            {
                                // Create formatted text for emotion label
                                FormattedText text = new FormattedText(
                                    emotionLabel,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    FlowDirection.LeftToRight,
                                    new Typeface("Segoe UI"),
                                    32,
                                    Brushes.Lime,
                                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                                // Position text above face rectangle, clamp Y to 0
                                Point textPos = new Point(face.X, Math.Max(face.Y - text.Height - 5, 0));

                                // Draw emotion text
                                dc.DrawText(text, textPos);

                                // Fire emotion event
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

            // Render the DrawingVisual to a RenderTargetBitmap
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);
            renderBitmap.Freeze();

            // Fire overlay 
            universalKinect.bitmapArrived?.Invoke(this, new BitmapArrivedEventArgs(renderBitmap, BitmapArrivedEventArgs.Type.Emotion));
        }



        /// <summary>
        /// Draws joint positions as ellipses over a blank canvas and sends the result as a Bitmap.
        /// </summary>
        private void generateImageWithSkeletonOverlay(Body[] bodies)
        {
            DrawingVisual drawingVisual = new DrawingVisual();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
                {
                    Body body = bodies[bodyIndex];
                    SolidColorBrush solidColorBrush = UniversalKinect.BrushForBody(bodyIndex);

                    foreach (JointType jointType in body.Joints.Keys)
                    {
                        // Get 3D joint position
                        CameraSpacePoint position = body.Joints[jointType].Position;

                        // Convert to 2D image space
                        ColorSpacePoint colorSpacePoint = coordinateMapper.MapCameraPointToColorSpace(position);
                        Point point = new Point(colorSpacePoint.X, colorSpacePoint.Y);

                        // Draw the joint as a circle
                        drawingContext.DrawEllipse(solidColorBrush, null, point, 10, 10);
                    }
                }
            }

            // Render drawing to bitmap
            RenderTargetBitmap bitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);
            bitmap.Freeze();

            // Fire overlay event
            universalKinect.bitmapArrived?.Invoke(this, new BitmapArrivedEventArgs(bitmap, BitmapArrivedEventArgs.Type.Skeleton));
        }
    }
}
