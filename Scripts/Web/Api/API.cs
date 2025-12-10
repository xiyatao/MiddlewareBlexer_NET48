using Kinect_Middleware.Models;
using Kinect_Middleware.Models.Web;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// Class that shortens the code needed to send or retrieve informations from the server
    /// </summary>
    internal class API {
        public static WebLoginResponse Login(string login, string password) {
            string[] argumentsArray = { Uri.EscapeDataString(login), Uri.EscapeDataString(password) };
            string arguments = string.Join("/-*-/", argumentsArray).Replace('+', ' ');

            HttpWebResponse response = SimpleWebRequest.Request(URLs.LoadSettingsByUser, arguments);

            string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            string decryptedString = "";

            try {
                decryptedString = Encryption.DecryptString(responseString, Encryption.InitializationVector);
            } catch (Exception ex) {
                // The login has not been completed
            }

            return new WebLoginResponse(responseString, decryptedString);
        }

        public static string SendResult(
            int id_user,
            ExerciseResult result
        ) {
            try {
                var request = (HttpWebRequest)WebRequest.Create(URLs.SaveResult);

                string postData = result.GenerateUrlData(id_user);
                postData = result.id_setting + " &data=" + Encryption.EncryptString(postData, Encryption.InitializationVector);

                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream()) {
                    stream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                return new StreamReader(response.GetResponseStream()).ReadToEnd();
            } catch (WebException exception) {
                throw new WebException();
            }
        }
    }
}
