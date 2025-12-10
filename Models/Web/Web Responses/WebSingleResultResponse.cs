namespace Kinect_Middleware.Models.Web.Web_Responses {
    /// <summary>
    /// Class allows you to easily read the response to the result POST
    /// </summary>
    internal class WebSingleResultResponse {
        public enum Status {
            OK,
            SentError,
            DBConnectionError,
            Unknown
        }

        public string Response;

        public WebSingleResultResponse(string response) {
            this.Response = response;
        }

        public static explicit operator WebSingleResultResponse(string value) {
            return new WebSingleResultResponse(value);
        }

        public Status ResponseStatus {
            get {
                if (Response.Equals("OK")) {
                    return Status.OK;
                } else if (Response.Equals("SQL_ERROR")) {
                    return Status.SentError;
                } else if (Response.Equals("CONEX_ERROR")) {
                    return Status.DBConnectionError;
                } else {
                    return Status.Unknown;
                }
            }
        }
    }
}
