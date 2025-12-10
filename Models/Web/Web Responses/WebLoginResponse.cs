namespace Kinect_Middleware.Models.Web {
    /// <summary>
    /// Class allows you to easily read the response to the user's login request
    /// </summary>
    internal class WebLoginResponse {
        public enum Status {
            Valid,
            IncorrectLoginOrPassword,
            AccountDoesNotExist,
            DataNotValid,
            InternalDatabaseError
        }

        public WebLoginResponse(string response, string decrypted) {
            this.Response = response;
            this.Decrypted = decrypted;
        }

        public string Response;
        public string Decrypted;

        public Status ResponseStatus {
            get {
                if (Response.Length > 3) {
                    return Status.Valid;
                } else {
                    if (Response.Equals("e01")) {
                        return Status.IncorrectLoginOrPassword;
                    } else if (Response.Equals("e02")) {
                        return Status.AccountDoesNotExist;
                    } else if (Response.Equals("e03")) {
                        return Status.DataNotValid;
                    } else {
                        return Status.InternalDatabaseError;
                    }
                }
            }
        }
    }
}
