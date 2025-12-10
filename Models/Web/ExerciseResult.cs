namespace Kinect_Middleware.Models {
    public class ExerciseResult {
        public string date;
        public int id_exercise;
        public string code_exercise;
        public int id_setting;
        public string param1;
        public string param2;
        public string param3;
        public string param4;
        public int duration;
        public int attempts;
        public int corrects;
        public int achieved;

        public string GenerateUrlData(int userId) {
            string[] array = {
                userId.ToString(),
                date,
                id_exercise.ToString(),
                code_exercise,
                id_setting.ToString(),
                param1 == "" ? "null" : param1,
                param2 == "" ? "null" : param2,
                param3 == "" ? "null" : param3,
                param4 == "" ? "null" : param4,
                duration.ToString(),
                attempts.ToString(),
                corrects.ToString(),
                achieved.ToString()
            };

            return string.Join("/-*-/", array);
        }
    }
}
