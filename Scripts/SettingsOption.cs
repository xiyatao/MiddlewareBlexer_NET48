using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Kinect_Middleware.Scripts {
    /// <summary>
    /// Used to create and save the values for ComboBox selection
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SettingsOption<T> {
        private Dictionary<string, T> Dictionary;
        private bool translate;

        [JsonProperty]
        public int Index = 0;

        public IEnumerable<string> Options {
            get {
                if (translate) {
                    AppSettings appSetting = App.Host.Services.GetService<AppSettings>();

                    return Dictionary.Keys.Select(key => appSetting.Translate(key));
                } else {
                    return Dictionary.Keys;
                }
            }
        }

        public T Value {
            get {
                return Dictionary.ElementAt(Index).Value;
            }
        }

        public SettingsOption(Dictionary<string, T> dictionary, int initialIndex, bool translate = false) {
            this.Dictionary = dictionary;
            this.Index = initialIndex;
            this.translate = translate;
        }
    }
}
