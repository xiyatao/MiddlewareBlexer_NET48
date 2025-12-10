/*
===================================================================================
 Author          : Xiya Tao
 Institution     : Universidad Politécnica de Madrid (UPM)
 Modification    : Central orchestration of Kinect, Emotion, and Sensor data fusion.
                   - Merged sensor and emotion data handling into MainWindow
                   - Listens to both Kinect emotion events and TCP sensor events
                   - Combines joint, emotion, and sensor data into a unified JSON
                   - Saves emotion and sensor data separately to CSV
                   - Enqueues enriched JSON payload for UDP transmission
                   - Performs temporal alignment of multimodal data (100 ms window)
                   - Supports asynchronous, thread-safe logging and dispatching
                   - Handles UDP handshake and reconnection with Unity consumer
                   - Updates WPF UI bindings through MainWindowViewModel
                   - Provides optional DataFusionBuffer for further fusion logic
 Purpose         : Serves as the main integration hub between motion capture,
                   physiological sensing, emotional inference, and network I/O.
 Last Modified   : 2025/11/03
===================================================================================
*/

using Kinect_Middleware.Kinect;
using Kinect_Middleware.Models;
using Kinect_Middleware.UDP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;          // for ToDictionary
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Kinect_Middleware.Logging;
using Kinect_Middleware.Views.Pages;
using Kinect_Middleware.Fusion;
using Kinect_Middleware.Core;
using Newtonsoft.Json.Linq;   // <<< 新增

namespace Kinect_Middleware
{
    public partial class MainWindow : Window
    {
        private UniversalKinect universalKinect;
        private TCPReceiver tcpReceiver;
        private UDPSend udpSender;
        private UDPReceive udpReceiver;
        private CSVLogger csvLogger;

        private Thread waitingThread;
        private bool sendingJoints = false;
        private bool firstConnection = true;
        private bool restartSending = false;

        private DataFusionBuffer fusionBuffer = new DataFusionBuffer();
        private TemporalAligner aligner = new TemporalAligner(100); // 100 ms window

        private Dictionary<string, MJoint> latestFrameData;
        private Dictionary<string, string> latestEmotionData;
        private Dictionary<string, string> latestSensorData;

        public MainWindow(
            UniversalKinect universalKinect,
            TCPReceiver tcpReceiver,
            UDPSend udpSender,
            UDPReceive udpReceiver)
        {
            InitializeComponent();

            csvLogger = new CSVLogger();

            this.universalKinect = universalKinect;
            this.udpSender = udpSender;
            this.udpReceiver = udpReceiver;
            this.tcpReceiver = tcpReceiver;

            // TCP HEARTRATE + SENSOR → CSV + Aligner
            _ = Task.Run(() =>
            {
                universalKinect.frameArrived += (sender, args) =>
                {
                    long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    latestFrameData = new Dictionary<string, MJoint>(args.dictionary);
                    csvLogger.LogKinect(latestFrameData);
                    aligner.AddSkeleton(ts, latestFrameData.ToDictionary(k => k.Key, v => (object)v.Value));
                };

                universalKinect.emotionArrived += (sender, args) =>
                {
                    long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    latestEmotionData = new Dictionary<string, string>(args.dictionary);
                    csvLogger.LogEmotion(latestEmotionData);
                    aligner.AddEmotion(ts, latestEmotionData.ToDictionary(k => k.Key, v => (object)v.Value));
                };

                //sensorDataArrived
                tcpReceiver.sensorDataArrived += (sender, args) =>
                {
                    long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    latestSensorData = new Dictionary<string, string>(args.dictionary);

                    csvLogger.LogSensor(latestSensorData);
                    aligner.AddSensor(ts, latestSensorData.ToDictionary(k => k.Key, v => (object)v.Value));
                };
            });

            // Kinect skeleton → UDP
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        string alignedJson = aligner.TryEmitAligned();
                        if (alignedJson != null)
                        {
                            string skeletonJson = null;

                            try
                            {
                                var root = JObject.Parse(alignedJson);
                                var kinectPart = root["Kinect"];
                                if (kinectPart != null)
                                    skeletonJson = kinectPart.ToString();
                            }
                            catch { }

                            if (skeletonJson == null && latestFrameData != null)
                                skeletonJson = JsonConvert.SerializeObject(latestFrameData);

                            if (!string.IsNullOrEmpty(skeletonJson))
                            {
                                skeletonJson = skeletonJson
                                    .Replace("\"X\"", "\"x\"")
                                    .Replace("\"Y\"", "\"y\"")
                                    .Replace("\"Z\"", "\"z\"")
                                    .Replace("\"W\"", "\"w\"");

                                universalKinect.Bindings.LastJSON = skeletonJson;
                                udpSender.messageQueue.Add(skeletonJson);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AlignerLoop] {ex.Message}");
                    }
                    await Task.Delay(50);
                }
            });

            // UDP shakehands loop
            StartConection();

            try
            {
                MainWindowViewModel.Instance.mainWindow = this;
                DataContext = MainWindowViewModel.Instance;
            }
            catch
            {
                DataContext = this;
            }
        }

        private void StartConection()
        {
            if (!firstConnection) return;
            try
            {
                waitingThread = new Thread(new ThreadStart(startKinect))
                {
                    IsBackground = true
                };
                waitingThread.Start();
                firstConnection = false;
            }
            catch (Exception err)
            {
                Console.WriteLine($"[StartConection] {err.Message}");
            }
        }

        private void startKinect()
        {
            bool isWait = false;
            while (true)
            {
                try
                {
                    isWait = udpReceiver.messageWaiting.Take();

                    if (isWait && !sendingJoints)
                    {
                        udpSender.sendPackets();
                        isWait = false;
                        sendingJoints = true;
                        restartSending = false;
                    }

                    if (isWait && sendingJoints && restartSending)
                    {
                        udpSender.sendPackets();
                        restartSending = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[startKinect] {ex.Message}");
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            csvLogger?.Dispose();
            base.OnClosed(e);
        }
    }
}
