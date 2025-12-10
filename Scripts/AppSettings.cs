using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// This class manages the settings of the whole application.   <br/>
    /// This includes:                                              <br/>
    ///     -language of the application
    /// </summary>
    internal class AppSettings {
        ResourceDictionary _dictionary;
        ResourceDictionary dictionary {
            get {
                if (_dictionary == null) {
                    _dictionary = getDictionary();
                }

                return _dictionary;
            }
            set {
                _dictionary = value;
            }
        }
        ///|////////////////////////////////////////////////////////////////////////////
        //|| Language
        ///|////////////////////////////////////////////////////////////////////////////

        public SettingsOption<string> Language = new SettingsOption<string>(
            dictionary: new Dictionary<string, string>(){
                {"English", "en"},
                {"Español", "es"}
            },
            initialIndex: 0
        );

        public void SwitchLanguage(ResourceDictionary resources) {
            dictionary = getDictionary();

            resources.MergedDictionaries.Add(dictionary);
        }

        private ResourceDictionary getDictionary() {
            ResourceDictionary dictionary = new ResourceDictionary();
            switch (Language.Value) {
                case "en":
                    dictionary.Source = new Uri("..\\Localization\\StringResources.en.xaml", UriKind.Relative);
                    break;
                case "es":
                    dictionary.Source = new Uri("..\\Localization\\StringResources.es.xaml", UriKind.Relative);
                    break;
            }

            return dictionary;
        }


        public string Translate(String key) {
            try {
                return (string)dictionary[key];
            } catch {
                return "";
            }
        }

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Saving / Loading
        ///|////////////////////////////////////////////////////////////////////////////

        private static String filename = "AppSettings.json";

        public void Save() {
            using (StreamWriter sw = new StreamWriter(filename)) {
                String json = JsonConvert.SerializeObject(this, Formatting.Indented);
                sw.Write(json);
            }
        }

        private AppSettings() { }

        static public AppSettings Read() {
            try {
                using (StreamReader sw = new StreamReader(filename)) {
                    string json = sw.ReadToEnd();

                    var pref = JsonConvert.DeserializeObject<AppSettings>(json);

                    if (pref == null) {
                        throw new Exception("Can not return empty object");
                    }

                    return pref;
                }
            } catch {
                return new AppSettings();
            }
        }
    }
}
