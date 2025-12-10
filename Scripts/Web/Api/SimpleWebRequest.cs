using System.Net;
using System.Text;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// Class that shortens the code needed to make web request
    /// </summary>
    internal class SimpleWebRequest {
        public static HttpWebResponse Request(
            string url,
            string dataString
        ) {
            var request = (HttpWebRequest)WebRequest.Create(url);
            string postData = "&data=" + Encryption.EncryptString(dataString, Encryption.InitializationVector);
            var dataBytes = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = dataBytes.Length;

            using (var stream = request.GetRequestStream()) {
                stream.Write(dataBytes, 0, dataBytes.Length);
            }

            return (HttpWebResponse)request.GetResponse();
        }
    }
}
