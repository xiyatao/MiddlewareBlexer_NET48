/*
===================================================================================
 Author          : Xiya Tao
 Institution     : Universidad Politécnica de Madrid (UPM)
 Module          : CSVLogger.cs
 Modification    : Fuses Kinect, Emotion, and multi-sensor data streams into synchronized
                   frame logs stored in CSV format. 
                   - Merges joint coordinates, emotion state, and physiological data
                   - Supports simultaneous recording from multiple devices (e.g. Polar H10, Bangle)
                   - Records one unified frame every 100 ms
                   - Ensures timestamp alignment with both local and UNIX time
                   - Handles missing or invalid sensor entries gracefully
                   - Provides thread-safe logging, periodic auto-flush, and per-device output columns
                   - Prints all connected device heart-rate values in csvfile for verification
 Last Modified   : 2025-11-03
===================================================================================
*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Timers;
using System.Windows;
using Kinect_Middleware.Models;

namespace Kinect_Middleware.Logging
{
    /// <summary>
    /// CSVLogger: fuses Kinect, Emotion, Sensor data and saves it to CSV in frames 
    /// Writes one line every 100ms with coordinates of each joint, expression, heart rate
    /// </summary>
    public class CSVLogger : IDisposable
    {
        private StreamWriter writer;
        private readonly object lockObj = new object();
        private readonly Timer flushTimer;
        private readonly string logPath;

        // Data caching
        private readonly Dictionary<string, MJoint> jointsBuffer = new Dictionary<string, MJoint>();
        private string emotionState = "";
        //private string heartRate = "";
        //private string respiration = "";
        private readonly Dictionary<string, string> heartRates = new Dictionary<string, string>();
        private readonly Dictionary<string, string> respirations = new Dictionary<string, string>();
        private string lastTimestamp;

        public CSVLogger(string folder = "Logs")
        {
            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                logPath = Path.Combine(folder, "FusionLog_" + timestamp + ".csv");

                writer = new StreamWriter(logPath, false, Encoding.UTF8);


                List<string> headers = new List<string>();
                headers.Add("timestamp_local");    
                headers.Add("timestamp_unix_ms");  

                string[] joints = new string[]
                {
                    "Head", "Neck", "SpineBase", "SpineMid",
                    "ShoulderLeft", "ShoulderRight",
                    "ElbowLeft", "ElbowRight",
                    "WristLeft", "WristRight",
                    "HipLeft", "HipRight",
                    "KneeLeft", "KneeRight",
                    "AnkleLeft", "AnkleRight"
                };

                foreach (string j in joints)
                {
                    headers.Add(j + "_X");
                    headers.Add(j + "_Y");
                    headers.Add(j + "_Z");
                }

                headers.Add("Emotion");

                // List<string> deviceNames = new List<string> { "PolarH10", "Bangle" };
                // foreach (var device in deviceNames)
                List<string> deviceNames = new List<string> { "PolarH10", "Bangle" };
                foreach (var device in deviceNames)
                {
                    headers.Add($"HeartRate_{device}");
                    headers.Add($"Respiration_{device}");
                }


                writer.WriteLine(string.Join(";", headers.ToArray()));
                writer.Flush();

                // every 100ms write one frame
                flushTimer = new Timer(100);
                flushTimer.Elapsed += FlushFrame;
                flushTimer.Start();

#if DEBUG
                MessageBox.Show("file path：\n" + Path.GetFullPath(logPath));
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CSVLogger] failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Kinect Data Entry (MJoint Dictionary)
        /// </summary>
        public void LogKinect(Dictionary<string, MJoint> joints)
        {
            lock (lockObj)
            {
                foreach (KeyValuePair<string, MJoint> kvp in joints)
                    jointsBuffer[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Emotion Data Entry
        /// </summary>
        public void LogEmotion(Dictionary<string, string> emotions)
        {
            lock (lockObj)
            {
                if (emotions.ContainsKey("emotion"))
                    emotionState = emotions["emotion"];
            }
        }

        /// <summary>
        /// Sensor data input (heart rate / respiration rate)
        /// </summary>
        public void LogSensor(Dictionary<string, string> sensors)
        {
            lock (lockObj)
            {
                if (!sensors.ContainsKey("DeviceName") || !sensors.ContainsKey("SensorData"))
                    return;

                string device = sensors["DeviceName"];
                string data = sensors["SensorData"];

                if (data.Contains("BPM:"))
                {
                    try
                    {
                        int bpmStart = data.IndexOf("BPM:") + 4;
                        int bpmEnd = data.IndexOf("RR");
                        string bpm = data.Substring(bpmStart, bpmEnd - bpmStart).Trim();
                        heartRates[device] = bpm;
                    }
                    catch { heartRates[device] = ""; }
                }

                if (data.Contains("RR:"))
                {
                    try
                    {
                        int rrStart = data.IndexOf("RR:") + 3;
                        string rr = data.Substring(rrStart).Trim(' ', '[', ']', ':');
                        respirations[device] = rr;
                    }
                    catch { respirations[device] = ""; }
                }
            }
        }


        /// <summary>
        /// Write one frame to CSV every 100ms
        /// </summary>
        private void FlushFrame(object sender, ElapsedEventArgs e)
        {
            lock (lockObj)
            {
                // ================================
                // 🕒 two new columns for timestamp
                // ================================
                string tsLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // local time
                long tsUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();      // UTC time in milliseconds

                // avoid duplicate frames
                if (tsLocal == lastTimestamp) return;
                lastTimestamp = tsLocal;

                List<string> line = new List<string>();
                line.Add(tsLocal);
                line.Add(tsUnix.ToString());

                string[] jointNames = new string[]
                {
                    "Head", "Neck", "SpineBase", "SpineMid",
                    "ShoulderLeft", "ShoulderRight",
                    "ElbowLeft", "ElbowRight",
                    "WristLeft", "WristRight",
                    "HipLeft", "HipRight",
                    "KneeLeft", "KneeRight",
                    "AnkleLeft", "AnkleRight"
                };

                foreach (string name in jointNames)
                {
                    if (jointsBuffer.ContainsKey(name))
                    {
                        MJoint j = jointsBuffer[name];
                        Vector3 pos = j.position;

                        if (float.IsNaN(pos.X) || float.IsNaN(pos.Y) || float.IsNaN(pos.Z))
                        {
                            line.Add(""); line.Add(""); line.Add("");
                        }
                        else
                        {
                            line.Add(pos.X.ToString("F4"));
                            line.Add(pos.Y.ToString("F4"));
                            line.Add(pos.Z.ToString("F4"));
                        }
                    }
                    else
                    {
                        line.Add(""); line.Add(""); line.Add("");
                    }
                }

                line.Add(emotionState);

                // for each device, add heart rate and respiration
                foreach (var device in heartRates.Keys.Union(respirations.Keys))
                {
                    heartRates.TryGetValue(device, out string hr);
                    respirations.TryGetValue(device, out string rr);
                    line.Add(hr);
                    line.Add(rr);
                }


                writer.WriteLine(string.Join(";", line.ToArray()));
                writer.Flush();

                string hrSummary = string.Join("; ", heartRates.Select(kv => $"{kv.Key}:{kv.Value}"));
                Console.WriteLine($"[CSVLogger] Frame written: {tsLocal}, UNIX={tsUnix}, Joints={jointsBuffer.Count}, Emotion={emotionState}, HRs=[{hrSummary}]");


            }
        }

        public void Dispose()
        {
            flushTimer.Stop();
            flushTimer.Dispose();
            writer.Flush();
            writer.Close();
        }
    }
}
