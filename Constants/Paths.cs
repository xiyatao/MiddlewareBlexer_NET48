/*
===================================================================================
 Author          : ---
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : ---
 Description     : Centralized class for all file and folder paths used in the 
                   application, including login, settings, user results, and 
                   facial emotion recognition (FER) models and logs.
 Last Modified   : 2025-08-13
===================================================================================
*/

namespace Kinect_Middleware.Models
{
    /// <summary>
    /// Holds all file and folder paths used throughout the application.
    /// Provides centralized access to paths for login, settings, results, and model files.
    /// </summary>
    internal class Paths
    {
        // Base folder for all application files
        public static string BasePath = @".\k2umfiles";

        // Folder where login-related files are stored
        public static string LoginFolderPath
        {
            get { return $@"{BasePath}\login"; }
        }

        // Path to the login information file
        public static string LoginPath
        {
            get { return $@"{LoginFolderPath}\login.txt"; }
        }

        // Folder where user settings are saved
        public static string SettingFolderPath
        {
            get { return $@"{BasePath}\users-settings"; }
        }

        // Path to a specific user's settings file based on login
        public static string SettingsPath(string login)
        {
            return $@"{SettingFolderPath}\{login}.txt";
        }

        // Folder where user results are stored
        public static string UserResultsFolderPath
        {
            get { return $@"{BasePath}\users-results\"; }
        }

        // Path to a specific user's results file
        public static string UserResultsPath(string login)
        {
            return $@"{UserResultsFolderPath}\{login}.txt";
        }

        // Path to the CSV file logging sensor data
        public static string SensorDataLog
        {
            get { return $@"SensorDataLog.csv"; }
        }

        // Folder used for facial emotion recognition (FER) resources
        public static string FerFolderPath
        {
            get { return $@"{BasePath}\fer"; }
        }

        // Path to the FER emotion recognition ONNX model
        public static string FerModelPath
        {
            get { return $@"{FerFolderPath}\emotion_model_4class_refined_dataset.onnx"; }
        }

        // Path to the Caffe face detection model file
        public static string CaffeModelPath
        {
            get { return $@"{FerFolderPath}\res10_300x300_ssd_iter_140000.caffemodel"; }
        }

        // Path to the Caffe deploy prototxt configuration file
        public static string DeployPrototxtPath
        {
            get { return $@"{FerFolderPath}\deploy.prototxt"; }
        }

        // Path to the CSV file logging detected emotions
        public static string EmotionLog
        {
            get { return $@"EmotionLog.csv"; }
        }

        // Private constructor to prevent instantiation
        private Paths() { }
    }
}
