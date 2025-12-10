/*
    ===================================================================================
    Author          : Marilena Tsami
    Institution     : Universidad Politécnica de Madrid (UPM)
    Course          : Master in Internet of Things (IoT) – 2024/2025
    Description     : EmotionPredictor class that uses an ONNX model to detect emotions
                      (positive, neutral, surprise, negative) from 72x72 grayscale 
                      facial images using EmguCV and ONNX Runtime.
    Last Updated    : 2025-05-25
    ===================================================================================
*/


using Emgu.CV;
using Emgu.CV.Structure;
using Kinect_Middleware.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.IO;
using System.Linq;


namespace EmotionDetector {
    public class EmotionPredictor {
        // ONNX runtime session used to load and run the emotion detection model
        private InferenceSession _session;

        // The possible emotion labels corresponding to the model's output classes
        // Important not to change the order of these labels, as they correspond to the model's output indices!
        private readonly string[] _labels = { "positive", "neutral", "surprise", "negative" };

        // Constructor that loads the ONNX model from the given file path
        public EmotionPredictor() {
            string modelPath = Paths.FerModelPath;

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"ONNX model not found: {modelPath}");

            try {
                _session = new InferenceSession(modelPath);
            } catch (Exception ex) {
                Console.WriteLine($"Failed to load ONNX model: {ex.Message}");
            }

        }

        /// <summary>
        /// Predict the emotion of the input grayscale face image.
        /// </summary>
        /// <param name="input">72x72 grayscale Emgu CV image</param>
        /// <returns>Emotion label as a string</returns>
        public string Predict(Image<Gray, byte> input) {
            try {
                // Verify that the input image has the expected dimensions 72x72 pixels.
                // If not, throw an exception to prevent incorrect model input.
                if (input.Width != 72 || input.Height != 72)
                    throw new ArgumentException($"Input image must be 72x72 but was {input.Width}x{input.Height}");

                // Create an array to hold normalized pixel values.
                // The model expects pixel values scaled between 0 and 1 (float).
                float[] data = new float[72 * 72];

                // Loop through each pixel in the grayscale image.
                // Normalize pixel intensity from byte [0..255] to float [0..1].
                for (int y = 0; y < 72; y++) {
                    for (int x = 0; x < 72; x++) {
                        data[y * 72 + x] = input.Data[y, x, 0] / 255.0f;
                    }
                }

                // Create a tensor with shape [1, 72, 72, 1]:
                // 1 = batch size,
                // 72 = height,
                // 72 = width,
                // 1 = number of channels (grayscale)
                var tensor = new DenseTensor<float>(new[] { 1, 72, 72, 1 });

                // Copy normalized pixel data into the tensor buffer efficiently.
                data.AsSpan().CopyTo(tensor.Buffer.Span);

                // Retrieve the model's input node name dynamically.
                // This avoids hardcoding and errors if input name changes.
                string inputName = _session.InputMetadata.Keys.FirstOrDefault();

                // If model has no inputs, throw an exception.
                if (inputName == null)
                    throw new Exception("Model has no inputs.");

                // Create the named ONNX input value required by the InferenceSession.
                var inputTensor = NamedOnnxValue.CreateFromTensor(inputName, tensor);

                // Run the model inference using the prepared input tensor.
                // The result is a collection of output tensors (usually one).
                IDisposableReadOnlyCollection<DisposableNamedOnnxValue> disposableNamedOnnxValues = _session.Run(new[] { inputTensor });

                // Extract the first output tensor as a float array.
                var output = disposableNamedOnnxValues.First().AsEnumerable<float>().ToArray();
                                
                // Find the index of the maximum value in the output array,
                // which corresponds to the predicted emotion class.
                int maxIndex = Array.IndexOf(output, output.Max());

                // Return the emotion label string matching the highest score.
                return _labels[maxIndex];

            } catch (Exception ex) {
                // If any error occurs during processing, log it and return "Error".
                Console.WriteLine($"Error during prediction: {ex.Message}");
                return "Error";
            }
        }

    }
}
