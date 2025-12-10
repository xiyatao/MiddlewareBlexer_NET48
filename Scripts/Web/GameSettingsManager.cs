using Kinect_Middleware.Models;
using Kinect_Middleware.UDP;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Kinect_Middleware.Scripts.Web {
    /// <summary>
    /// The main task of this class is to send settings related to a given game
    /// </summary>
    internal class GameSettingsManager {
        private string login;
        private string gameInfo;

        public GameSettingsManager(string login, string[] game) {
            this.login = login;
            this.gameInfo = "[" + game[0] + ", " + game[1] + ", " + game[2] + "]";
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| PUBLIC
        ///|////////////////////////////////////////////////////////////////////////////

        public LoadSettingResponse SendSettings() {
            LoadSettingResponse loadSettingResponse = deserializeConfiguration();

            if (loadSettingResponse == null)
                return null;

            sendSerializeSettings(loadSettingResponse);
            return loadSettingResponse;
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| PRIVATE
        ///|////////////////////////////////////////////////////////////////////////////

        // Deserializes the string with the configuration data and converts it into an object of type LoadSettingResponse
        private LoadSettingResponse deserializeConfiguration() {
            string settings = FileActions.TryReadFile(Paths.SettingsPath(login));

            if (settings == "") {
                return null;
            }

            LoadSettingResponse loadSettingResponse = JsonConvert.DeserializeObject<LoadSettingResponse>(settings);

            loadSettingResponse.gameRequest = deserializeGameRequested(loadSettingResponse);

            return loadSettingResponse;
        }

        // Deserialize an object of type Game from the JSON of the web
        private Game deserializeGameRequested(LoadSettingResponse loadSettingResponse) {
            List<Exercise> exerciseList = new List<Exercise>();

            foreach (var pair in loadSettingResponse.gamesMap[gameInfo]) {
                String exerciseName = pair.Key;
                Setting exerciseSetting = pair.Value;

                exerciseList.Add(new Exercise(exerciseName, exerciseSetting));
            }

            Game requestedGame = new Game();
            requestedGame.gameInfo = gameInfo;
            requestedGame.exercises = exerciseList.ToArray();


            return requestedGame;
        }

        private void sendSerializeSettings(LoadSettingResponse loadSettingResponse) {
            string settings = JsonConvert.SerializeObject(loadSettingResponse);
            App.Host.Services.GetRequiredService<UDPSend>().SendGameSettings(settings);
        }
    }
}
