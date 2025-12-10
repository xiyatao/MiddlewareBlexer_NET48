/*
    ===================================================================================
    Author          : Marilena Tsami
    Institution     : Universidad Politécnica de Madrid (UPM)
    Course          : Master in Internet of Things (IoT) – 2024/2025
    Description     : FaceDetector class that uses a pre-trained DNN model (Caffe) to 
                      detect faces in a video frame and selects the most prominent one 
                      based on size and proximity to the center.
    Last Updated    : 2025-05-25
    ===================================================================================
*/


using Emgu.CV;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Kinect_Middleware.Models;
using System;
using System.Drawing;

namespace EmotionDetector
{
    public class FaceDetector
    {
        private Net faceNet;
        private float confidenceThreshold = 0.5f;

        public FaceDetector()
        {
            // Load pre-trained Caffe face detection model
            string modelFile = Paths.CaffeModelPath;
            string configFile = Paths.DeployPrototxtPath;

            faceNet = DnnInvoke.ReadNetFromCaffe(configFile, modelFile);
        }

        /// <summary>
        /// Detects the most prominent face in the image using a Caffe DNN model.
        /// Accepts an EmguCV image in BGRA format.
        /// </summary>
        /// <param name="bgraImage">Input image in BGRA format (EmguCV).</param>
        /// <returns>The bounding box of the best face detected, or null if none found.</returns>
        public Rectangle? DetectBestFace(Image<Bgra, byte> bgraImage)
        {
            // Validate the input
            if (bgraImage == null || bgraImage.Width == 0 || bgraImage.Height == 0)
                return null;

            // Convert BGRA image to BGR (required by the DNN)
            Mat bgrMat = new Mat();
            CvInvoke.CvtColor(bgraImage, bgrMat, Emgu.CV.CvEnum.ColorConversion.Bgra2Bgr);

            // Convert the image to a blob suitable for DNN input
            // Resize to 300x300, subtract mean, keep scale, no cropping
            Mat blob = DnnInvoke.BlobFromImage(bgrMat, 1.0, new Size(300, 300), new MCvScalar(104, 177, 123), false, false);

            // Feed the blob into the network
            faceNet.SetInput(blob);
            Mat detections = faceNet.Forward();

            Rectangle? bestFace = null;
            double bestArea = 0;
            double bestCenterDistance = double.MaxValue;

            int frameWidth = bgrMat.Width;
            int frameHeight = bgrMat.Height;
            Point imageCenter = new Point(frameWidth / 2, frameHeight / 2);

            // Each detection has format: [batchId, classId, confidence, x1, y1, x2, y2]
            for (int i = 0; i < detections.SizeOfDimension[2]; i++)
            {
                float confidence = detections.GetData().GetValue(0, 0, i, 2) is float val ? val : 0;

                if (confidence >= confidenceThreshold)
                {
                    int width = bgrMat.Width;
                    int height = bgrMat.Height;

                    float x1 = (float)detections.GetData().GetValue(0, 0, i, 3);
                    float y1 = (float)detections.GetData().GetValue(0, 0, i, 4);
                    float x2 = (float)detections.GetData().GetValue(0, 0, i, 5);
                    float y2 = (float)detections.GetData().GetValue(0, 0, i, 6);

                    Rectangle face = new Rectangle(
                        (int)(x1 * width),
                        (int)(y1 * height),
                        (int)((x2 - x1) * width),
                        (int)((y2 - y1) * height)
                    );

                    double area = face.Width * face.Height;

                    Point faceCenter = new Point(face.X + face.Width / 2, face.Y + face.Height / 2);
                    double distanceToCenter = Math.Sqrt(
                        Math.Pow(faceCenter.X - imageCenter.X, 2) + Math.Pow(faceCenter.Y - imageCenter.Y, 2)
                    );

                    if (area > bestArea ||
                        (Math.Abs(area - bestArea) < 1e-2 && distanceToCenter < bestCenterDistance))
                    {
                        bestArea = area;
                        bestCenterDistance = distanceToCenter;
                        bestFace = face;
                    }
                }
            }

            return bestFace;
        }
    }
}
