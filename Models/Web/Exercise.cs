namespace Kinect_Middleware.Models {
    public class Exercise {
        public Exercise() {
        }
        public Exercise(string exerciseInfo, Setting setting) {
            this.ExerciseInfo = exerciseInfo;
            this.setting = setting;
        }

        public string ExerciseInfo;
        public Setting setting;
    }
}
