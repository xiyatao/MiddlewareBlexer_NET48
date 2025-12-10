using Kinect_Middleware.Models;
using Kinect_Middleware.Models.Web.Web_Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kinect_Middleware.Scripts.Web {
    /// <summary>
    /// This class is used to manage the results of the exercise 
    /// by saving them in local memory and sending them to the server
    /// </summary>
    internal class UserResultsManager {
        private ShowMessage showMessage = new ShowMessage("LOGIN (Results)");
        private List<ExerciseResult> list = new List<ExerciseResult>();

        ///|////////////////////////////////////////////////////////////////////////////
        //|| PUBLIC
        ///|////////////////////////////////////////////////////////////////////////////

        public void AddResult(string json) {
            ExerciseResult result = JsonConvert.DeserializeObject<ExerciseResult>(json);
            if (result != null)
                list.Add(result);
        }

        public void GenerateResults(string username, int id_user) {
            string userResultsPath = Paths.UserResultsPath(username);

            FileActions.TryDeleteFile(userResultsPath);

            UserResults results = new UserResults();
            results.id_user = id_user;
            results.resultsArray = list.ToArray();

            saveResults(username, results);
        }

        public void SendUnsubmittedResults(string username) {
            UserResults userResults;
            try {
                userResults = loadUserResults(username);
            } catch (Exception exception) {
                showMessage.Error(exception.Message);
                return;
            }

            List<ExerciseResult> unsentResults = sendUserResults(userResults);

            bool allResultsSent = (unsentResults.Count == 0);

            if (allResultsSent) {
                showMessage.Information("Saved results were found and sent to the web.");
                FileActions.TryDeleteFile(Paths.UserResultsPath(username));
            } else {
                showMessage.Error("Some error occure while sending results to the web.");

                bool deleteSentResults = (unsentResults.Count == userResults.resultsArray.Length);

                if (deleteSentResults) {
                    // We create an object of type UserResults to later serialize it and save it in the results file:
                    UserResults userResultsNotDeleted = new UserResults();
                    userResultsNotDeleted.resultsArray = unsentResults.ToArray();
                    userResultsNotDeleted.id_user = userResults.id_user;

                    saveResults(username, userResultsNotDeleted);
                }
            }
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| PRIVATE
        ///|////////////////////////////////////////////////////////////////////////////

        private void saveResults(string username, UserResults results) {
            // Indicates if the results have already been sent or not
            string json = JsonConvert.SerializeObject(results);

            // Create directory if needed
            Directory.CreateDirectory(Paths.UserResultsFolderPath);
            // Save results
            File.WriteAllText(Paths.UserResultsPath(username), json);
        }

        private UserResults loadUserResults(string username) {
            string resultFilePath = Paths.UserResultsPath(username);
            string json = FileActions.TryReadFile(resultFilePath);

            if (json == "") {
                FileActions.TryDeleteFile(resultFilePath);

                throw new Exception("This user has no pending results to send");
            }

            try {
                return JsonConvert.DeserializeObject<UserResults>(json);
            } catch (JsonException) {
                throw new Exception(
                    @"This user has badly formatted saved results.
                      Unable to send to the web."
                );
            }
        }

        private List<ExerciseResult> sendUserResults(UserResults userResults) {
            ExerciseResult[] results = userResults.resultsArray;
            List<ExerciseResult> unsentResults = new List<ExerciseResult>();

            int userID = userResults.id_user;
            int numberOfResults = results.Length;

            if (numberOfResults == 0) {
                return new List<ExerciseResult>();
            }

            // GO THROUGH THE ARRAY OF RESULTS READ FROM THE JSON
            foreach (ExerciseResult result in results) {
                if (result == null) {
                    continue; // Skip the iteration for this element
                }

                try {
                    WebSingleResultResponse response = (WebSingleResultResponse)API.SendResult(userID, result);

                    switch (response.ResponseStatus) {
                        case WebSingleResultResponse.Status.SentError:
                        case WebSingleResultResponse.Status.DBConnectionError:
                            unsentResults.Add(result);
                            break;
                        default:
                            break;
                    }
                } catch {
                    // Error occure 
                    unsentResults.Add(result);
                }

            }

            return unsentResults;
        }
    }
}
