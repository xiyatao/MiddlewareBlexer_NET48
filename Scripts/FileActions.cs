using System.IO;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// Class for operations on files without using try and catch
    /// </summary>
    internal class FileActions {
        public static string TryReadFile(string filePath) {
            try {
                return File.ReadAllText(filePath);
            } catch {
                // In case of any exception return empty string
                return "";
            }
        }

        public static void TryDeleteFile(string filePath) {
            try {
                File.Delete(filePath);
            } catch {
                // The file to be deleted was not found.
            }
        }
    }
}
