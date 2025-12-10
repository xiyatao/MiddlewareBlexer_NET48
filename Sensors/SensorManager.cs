/*
===================================================================================
 Author          : Fedi Khayati
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : Master in Internet of Things (IoT) – 2024/2025
 Description     : Manages connection and disconnection of sensors such as heart rate 
                   monitors and accelerometers, launching external apps and sending TCP 
                   messages asynchronously.
 Last Modified   : 2025-08-13
===================================================================================
*/

using Kinect_Middleware.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace Kinect_Middleware.Sensors
{
    /// <summary>
    /// Handles connecting and disconnecting various sensors, launching external apps via URI,
    /// and sending TCP messages asynchronously to communicate with middleware.
    /// Compatible with C# 7.3.
    /// </summary>
    public class SensorManager
    {
        // --------------------- internal state ---------------------
        private bool isBangleAppLaunched = false;
        private bool isAccelerometerBangleActive = false;

        // key = sensor name ("Polar H10", "Bangle watch"), value = active flag
        private readonly Dictionary<string, bool> activeHeartRates =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // --------------------- heart-rate control ---------------------
        public async Task ConnectHeartRateSensor(string sensorName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sensorName))
                {
                    MessageBox.Show("Please select a valid heart rate sensor.", "Unknown Sensor",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool alreadyActive;
                if (activeHeartRates.TryGetValue(sensorName, out alreadyActive) && alreadyActive)
                {
                    MessageBox.Show(sensorName + " is already active.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (sensorName.Equals("Polar H10", StringComparison.OrdinalIgnoreCase))
                {
                    LaunchUri("mypolarapp://");
                    await Task.Delay(1000);

                    bool ok = await SendTcpMessageAsync("HeartRatePolarH10");
                    if (!ok)
                        throw new Exception("Unable to send HeartRatePolarH10 handshake");

                    activeHeartRates["Polar H10"] = true;
                }
                else if (sensorName.Equals("Bangle watch", StringComparison.OrdinalIgnoreCase))
                {
                    if (!isBangleAppLaunched)
                    {
                        LaunchUri("mybangleapp://");
                        isBangleAppLaunched = true;
                    }

                    await Task.Delay(1000);

                    bool sent = await SendTcpMessageAsync("HeartRateBangle");
                    if (!sent)
                        throw new Exception("Unable to send HeartRateBangle handshake");

                    activeHeartRates["Bangle watch"] = true;
                }
                else
                {
                    MessageBox.Show("Please select a valid heart rate sensor.", "Unknown Sensor",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show(sensorName + " connected successfully.",
                    "Heart Rate", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to heart rate sensor: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void DisconnectHeartRatePolarH10Sensor(bool silent = false)
        {
            await SendTcpMessageAsync("HeartRatePolarH10Shutdown");
            activeHeartRates["Polar H10"] = false;
            if (!silent)
                MessageBox.Show("Stopped listening to Polar H10 heart rate.",
                    "Heart Rate", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async void DisconnectHeartRateBangleSensor(bool silent = false)
        {
            await SendTcpMessageAsync("HeartRateBangleShutdownOnly");
            activeHeartRates["Bangle watch"] = false;
            if (!silent)
                MessageBox.Show("Stopped listening to Bangle watch heart rate.",
                    "Heart Rate", MessageBoxButton.OK, MessageBoxImage.Information);
            CheckIfShouldShutdownBangleApp();
        }

        public void DisconnectHeartRateSensor(string sensorName)
        {
            try
            {
                bool active;
                if (!activeHeartRates.TryGetValue(sensorName, out active) || !active)
                {
                    MessageBox.Show(sensorName + " is not active.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (sensorName.Equals("Polar H10", StringComparison.OrdinalIgnoreCase))
                    DisconnectHeartRatePolarH10Sensor();
                else if (sensorName.Equals("Bangle watch", StringComparison.OrdinalIgnoreCase))
                    DisconnectHeartRateBangleSensor();
                else
                    MessageBox.Show("Please select a valid heart rate sensor.", "Unknown Sensor",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool IsHeartRateActive(string sensorName)
        {
            bool active;
            return activeHeartRates.TryGetValue(sensorName, out active) && active;
        }

        // --------------------- accelerometer control ---------------------
        public async Task ConnectAccelerometerSensor(string sensorName)
        {
            try
            {
                if (sensorName == "Bangle watch")
                {
                    if (!isBangleAppLaunched)
                    {
                        LaunchUri("mybangleapp://");
                        isBangleAppLaunched = true;
                    }

                    await Task.Delay(1000);
                    bool sent = await SendTcpMessageAsync("AccelerometerBangle");
                    if (!sent)
                    {
                        MessageBox.Show("Failed to send TCP message to Bangle watch (Accelerometer).",
                            "TCP Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    isAccelerometerBangleActive = true;
                }
                else
                {
                    MessageBox.Show("Please select a valid accelerometer sensor.",
                        "Unknown Sensor", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to accelerometer sensor: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void DisconnectAccelerometerBangleSensor(bool silent = false)
        {
            await SendTcpMessageAsync("AccelerometerBangleShutdownOnly");
            isAccelerometerBangleActive = false;
            if (!silent)
                MessageBox.Show("Stopped listening to Bangle accelerometer.",
                    "Accelerometer", MessageBoxButton.OK, MessageBoxImage.Information);
            CheckIfShouldShutdownBangleApp();
        }

        public void DisconnectAccelerometerSensor(string sensorName)
        {
            try
            {
                if (sensorName == "Bangle watch")
                    DisconnectAccelerometerBangleSensor();
                else
                    MessageBox.Show("Please select a valid accelerometer sensor.",
                        "Unknown Sensor", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------- shared helpers ---------------------
        private void LaunchUri(string uri)
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void CheckIfShouldShutdownBangleApp()
        {
            bool bangleHrActive;
            bool hrActive = activeHeartRates.TryGetValue("Bangle watch", out bangleHrActive) && bangleHrActive;
            if (!hrActive && !isAccelerometerBangleActive)
                isBangleAppLaunched = false;
        }

        public void ShutdownAllSensors()
        {
            bool hrBangle, hrPolar;
            if (activeHeartRates.TryGetValue("Bangle watch", out hrBangle) && hrBangle)
                DisconnectHeartRateBangleSensor(true);
            if (isAccelerometerBangleActive)
                DisconnectAccelerometerBangleSensor(true);
            if (activeHeartRates.TryGetValue("Polar H10", out hrPolar) && hrPolar)
                DisconnectHeartRatePolarH10Sensor(true);
        }

        // --------------------- TCP utility ---------------------
        public async Task<bool> SendTcpMessageAsync(string message)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(TCPConstants.TcpServerIp, TCPConstants.TcpPort);
                    var timeoutTask = Task.Delay(3000);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    if (completedTask != connectTask)
                        throw new TimeoutException("TCP connection timed out.");

                    using (NetworkStream stream = client.GetStream())
                    using (var writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        await writer.WriteLineAsync("SENDER_Middleware");
                        await writer.WriteLineAsync(message);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send TCP message: " + ex.Message);
                return false;
            }
        }
    }
}
