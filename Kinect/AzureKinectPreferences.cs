using Kinect_Middleware.Scripts;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kinect_Middleware.Kinect {
    /// <summary>
    /// Class responsible for storing and retriving preferences (settings) for Azure Kinect <br/>
    /// <br/>
    /// Information stored: <br/>
    ///     - Sensor orientation <br/>
    ///     - Processing mode (Cpu, Cuda, Gpu, DirectML, TensorRT) <br/>
    ///     - Resolution of image <br/>
    ///     - FPS of image <br/>
    /// </summary>
    public class AzureKinectPreferences {
        ///|////////////////////////////////////////////////////////////////////////////
        //|| SensorOrientation
        ///|////////////////////////////////////////////////////////////////////////////

        public SettingsOption<SensorOrientation> Orientation = new SettingsOption<SensorOrientation>(
            dictionary: new Dictionary<string, SensorOrientation>(){
                {"Default", SensorOrientation.Default},
                {"Flip180", SensorOrientation.Flip180},
                {"Clockwise90", SensorOrientation.Clockwise90},
                {"CounterClockwise90", SensorOrientation.CounterClockwise90}
            },
            initialIndex: 0,
            translate: true
        );

        ///|////////////////////////////////////////////////////////////////////////////
        //|| TrackerProcessingMode
        ///|////////////////////////////////////////////////////////////////////////////

        public SettingsOption<TrackerProcessingMode> ProcessingMode = new SettingsOption<TrackerProcessingMode>(
            dictionary: new Dictionary<string, TrackerProcessingMode>(){
                {"Cpu", TrackerProcessingMode.Cpu},
                {"Cuda", TrackerProcessingMode.Cuda},
                {"Gpu", TrackerProcessingMode.Gpu},
                {"DirectML", TrackerProcessingMode.DirectML},
                {"TensorRT",  TrackerProcessingMode.TensorRT}
            },
            initialIndex: 2
        );

        ///|////////////////////////////////////////////////////////////////////////////
        //|| ColorResolutionIndex
        ///|////////////////////////////////////////////////////////////////////////////

        public SettingsOption<ColorResolution> Resolution = new SettingsOption<ColorResolution>(
            dictionary: new Dictionary<string, ColorResolution>(){
                {"Off", ColorResolution.Off},
                {"720p", ColorResolution.R720p},
                {"1080p", ColorResolution.R1080p},
                {"1440p", ColorResolution.R1440p},
                {"1536p",  ColorResolution.R1536p},
                {"2160p",  ColorResolution.R2160p}
            },
            initialIndex: 1
        );

        ///|////////////////////////////////////////////////////////////////////////////
        //|| CameraFPS
        ///|////////////////////////////////////////////////////////////////////////////

        public SettingsOption<FPS> CameraFPS = new SettingsOption<FPS>(
            dictionary: new Dictionary<string, FPS>(){
                {"5 FPS", FPS.FPS5},
                {"15 FPS", FPS.FPS15},
                {"30 FPS", FPS.FPS30}
            },
            initialIndex: 2
        );

        ///|////////////////////////////////////////////////////////////////////////////
        //|| Saving / Loading
        ///|////////////////////////////////////////////////////////////////////////////

        private static String filename = "AzureKinectPreferences.json";

        public void Save() {
            using (StreamWriter sw = new StreamWriter(filename)) {
                String json = JsonConvert.SerializeObject(this, Formatting.Indented);
                sw.Write(json);
            }
        }

        private AzureKinectPreferences() { }

        static public AzureKinectPreferences Read() {
            try {
                using (StreamReader sw = new StreamReader(filename)) {
                    string json = sw.ReadToEnd();

                    var pref = JsonConvert.DeserializeObject<AzureKinectPreferences>(json);

                    if (pref == null) {
                        throw new Exception("Can not return empty object");
                    }

                    return pref;
                }
            } catch {
                return new AzureKinectPreferences();
            }
        }

    }
}
