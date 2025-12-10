using Kinect_Middleware.Models;
using Kinect_Middleware.Models.Web;
using Kinect_Middleware.Scripts;
using Kinect_Middleware.Scripts.Web;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net;

namespace Kinect_Middleware {
    /// <summary>
    /// This class is used to manage the user's login state and downloading 
    /// his configuration for this user and saving it in the device's memory
    /// </summary>
    internal class UserConfigurationManager {
        public string Username = "";
        public bool Authenticated = false;

        private ShowMessage showMessage = new ShowMessage("LOGIN (Confiiguration)");

        ///|////////////////////////////////////////////////////////////////////////////
        //|| PUBLIC
        ///|////////////////////////////////////////////////////////////////////////////

        public void LogIn(string username, string password) {
            if ((username == "") || (password == "")) {
                showMessage.Error("Fill in username and password");
            } else {
                try {
                    WebLoginResponse response = API.Login(username, password);


                    switch (response.ResponseStatus) {
                        case WebLoginResponse.Status.Valid:
                            string configuartion = response.Decrypted;

                            saveUsername(username);
                            saveUserConfiguration(username, configuartion);

                            App.Host.Services.GetRequiredService<UserResultsManager>()
                                             .SendUnsubmittedResults(username);

                            this.Username = username;
                            this.Authenticated = true;
                            break;
                        case WebLoginResponse.Status.IncorrectLoginOrPassword:
                            showMessage.Error("Incorrect login or password");
                            break;
                        case WebLoginResponse.Status.AccountDoesNotExist:
                            showMessage.Error("Incorrect login or password");
                            break;
                        case WebLoginResponse.Status.DataNotValid:
                            showMessage.Error("The data that has been submitted does not meet the requirements.");
                            break;
                        case WebLoginResponse.Status.InternalDatabaseError:
                            handleDBConnectionError(username);
                            break;
                    }
                } catch (WebException) {
                    handleWebException(username);
                }
            }
        }

        public void LogOut() {
            this.Username = "";
            this.Authenticated = false;
        }

        public string GetLastUsedUsername() {
            return FileActions.TryReadFile(Paths.LoginPath);
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| PRIVATE
        ///|////////////////////////////////////////////////////////////////////////////

        private void saveUsername(string username) {
            // Create directory if needes
            Directory.CreateDirectory(Paths.LoginFolderPath);
            // Write
            File.WriteAllText(Paths.LoginPath, username);
        }

        private void saveUserConfiguration(string username, string configuration) {
            if (configuration != null) {
                // Create directory if needed
                Directory.CreateDirectory(Paths.SettingFolderPath);
                // Write configuration to a file
                // NOTE: If the file already exists, it is overwritten
                File.WriteAllText(Paths.SettingsPath(username), configuration);

                showMessage.Information("Configuration downloaded!");
            }
        }

        private void handleDBConnectionError(string username) {
            // Error in connection with DB

            // If there is no connection to the web:
            // Check if there is a configuration file with the specified name
            bool isfile = File.Exists(Paths.SettingsPath(username));
            if (isfile) {
                // If there is, access the user anyway, but notify that
                // it is played without internet and with an old configuration
                File.WriteAllText(Paths.LoginPath, username);

                showMessage.Warning("Internet connection failed. Will play with previously downloaded settings.");
            } else {
                // If not, report the error and don't play
                // ERASE THE CONTENT OF THE CONFIGURATION FILE
                File.WriteAllText(Paths.LoginPath, "none");

                showMessage.Error("Error in connection with database");
            }
        }

        private void handleWebException(string username) {
            bool configurationExists = File.Exists(Paths.UserResultsPath(username));

            if (configurationExists) {
                File.WriteAllText(Paths.LoginPath, username);

                showMessage.Warning(
                    "Error connecting, please check your internet connection. \n" +
                    "It will play with the previously downloaded settings."
                );

                this.Username = username;
                this.Authenticated = true;
            } else {
                showMessage.Error(
                    "Error connecting, please check your internet connection. \n" +
                    "There is no configuration file available for this user, so you can't continue."
                );
            }
        }
    }
}
