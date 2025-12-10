using System.Collections.Generic;

namespace Kinect_Middleware.Models {
    public class LoadSettingResponse {
        public int id_user;
        public string login;
        public string first_name;
        public string last_name;
        public Game gameRequest;

        public Dictionary<string, Dictionary<string, Setting>> gamesMap {
            // This property will be ignored during serialization but included during deserialization
            internal get;
            set;
        }
    }
}
