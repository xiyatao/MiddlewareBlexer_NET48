/*
===================================================================================
 Author          : Marilena Tsami + Fedi Khayati
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : Master in Internet of Things (IoT) – 2024/2025
 Modification    : Merged sensor data functionality with emotion logging.
                   - Keeps emotion extraction and logging to EmotionLog.csv
                   - Adds HeartRate and Accelerometer tracking
                   - Adds SaveSensorDataToCSV for sensor data logging
 Last Modified   : 2025-08-13
===================================================================================
*/

using Kinect_Middleware.Models;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;

namespace Kinect_Middleware.Kinect
{
    /// <summary>
    /// Provides dynamic data bindings for Kinect and sensor information to the UI.
    /// Tracks Kinect devices, heart rate, accelerometer, and manages logging of emotions and sensor data.
    /// </summary>
    public class UniversalKinectBindings : INotifyPropertyChanged
    {
        // Kinect 1 connection state
        private bool isKinectOneRunning;
        public bool IsKinectOneRunning
        {
            get { return isKinectOneRunning; }
            set
            {
                isKinectOneRunning = value;
                OnPropertyChanged(nameof(IsKinectOneRunning));
                OnPropertyChanged(nameof(IsDisconnectedAvailable));
            }
        }

        // Azure Kinect connection state
        private bool isAzureKinectRunning;
        public bool IsAzureKinectRunning
        {
            get { return isAzureKinectRunning; }
            set
            {
                isAzureKinectRunning = value;
                OnPropertyChanged(nameof(IsAzureKinectRunning));
                OnPropertyChanged(nameof(IsDisconnectedAvailable));
            }
        }

        // True if either Kinect device is running
        public bool IsDisconnectedAvailable
        {
            get { return IsAzureKinectRunning || IsKinectOneRunning; }
        }

        // Last received JSON data from sensors or Kinect
        private string lastJSON;
        public string LastJSON
        {
            get { return lastJSON; }
            set
            {
                lastJSON = value;
                OnPropertyChanged(nameof(LastJSON));
            }
        }

        // Heart Rate sensor connection state
        private bool isHeartRateRunning;
        public bool IsHeartRateRunning
        {
            get => isHeartRateRunning;
            set
            {
                if (isHeartRateRunning == value) return;
                isHeartRateRunning = value;
                OnPropertyChanged(nameof(IsHeartRateRunning));
            }
        }

        // Accelerometer sensor connection state
        private bool isAccelerometerRunning;
        public bool IsAccelerometerRunning
        {
            get => isAccelerometerRunning;
            set
            {
                if (isAccelerometerRunning == value) return;
                isAccelerometerRunning = value;
                OnPropertyChanged(nameof(IsAccelerometerRunning));
            }
        }

        /// <summary>
        /// Extracts the emotion from the last received JSON and logs it to EmotionLog.csv with a timestamp.
        /// </summary>
        public void SaveEmotionToCSV()
        {
            string filePath = Paths.EmotionLog;

            if (string.IsNullOrEmpty(LastJSON))
                return;

            try
            {
                JObject parsedJson = JObject.Parse(LastJSON);

                if (parsedJson.TryGetValue("emotion", out JToken emotionToken))
                {
                    string emotion = emotionToken.ToString();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    bool fileExists = File.Exists(filePath);

                    using (StreamWriter writer = new StreamWriter(filePath, append: true))
                    {
                        if (!fileExists)
                        {
                            writer.WriteLine("Timestamp;Emotion");
                        }

                        writer.WriteLine($"{timestamp};{emotion}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to write emotion to CSV: " + ex.Message);
            }
        }

        /// <summary>
        /// Extracts sensor data from the last received JSON and logs it to SensorDataLog.csv with a timestamp.
        /// </summary>
        public void SaveSensorDataToCSV()
        {
            string filePath = Paths.SensorDataLog;

            if (string.IsNullOrEmpty(LastJSON))
                return;

            try
            {
                JObject parsedJson = JObject.Parse(LastJSON);

                if (parsedJson.TryGetValue("SensorData", out JToken sensorDataToken))
                {
                    string sensorData = sensorDataToken.ToString();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    bool fileExists = File.Exists(filePath);

                    using (StreamWriter writer = new StreamWriter(filePath, append: true))
                    {
                        if (!fileExists)
                        {
                            writer.WriteLine("Timestamp;Sensor Data");
                        }

                        writer.WriteLine($"{timestamp};{sensorData}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to write sensor data to CSV: " + ex.Message);
            }
        }

        // Event handler for notifying the UI when a property value changes
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
