/*
===================================================================================
 Author          : Fedi Khayati
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : Master in Internet of Things (IoT) – 2024/2025
 Description     : Receives TCP messages asynchronously from the middleware server,
                   parses heart rate (Polar H10, Bangle) and accelerometer data, 
                   and raises events for UI updates.
 Last Modified   : 2025-08-13
===================================================================================
*/

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Kinect_Middleware.Views.Pages
{
    /// <summary>
    /// Handles asynchronous TCP reception and parsing of sensor data.
    /// Raises events when new sensor data arrives.
    /// </summary>
    public partial class TCPReceiver : UserControl
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private StringBuilder _messageBuffer = new StringBuilder();

        // Event triggered when new sensor data is received
        public EventHandler<SensorDataArrivedEventArgs> sensorDataArrived;

        public TCPReceiver()
        {
            InitializeComponent();
            _ = ConnectToServerAsync();
        }

        /// <summary>
        /// Establishes TCP connection to the server and identifies as a receiver.
        /// </summary>
        private async Task ConnectToServerAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                UpdateStatus("Connecting to TCP server...");

                await _tcpClient.ConnectAsync("127.0.0.1", 12345);
                UpdateStatus("Connected to TCP server.");

                _networkStream = _tcpClient.GetStream();

                // Identify as a RECEIVER
                byte[] receiverIdMessage = Encoding.UTF8.GetBytes("RECEIVER\n");
                await _networkStream.WriteAsync(receiverIdMessage, 0, receiverIdMessage.Length);

                await ReceiveDataAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Continuously reads incoming TCP data, buffering incomplete lines.
        /// </summary>
        private async Task ReceiveDataAsync()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        UpdateStatus("Disconnected from server.");
                        break;
                    }

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _messageBuffer.Append(chunk);

                    string allText = _messageBuffer.ToString();
                    string[] lines = allText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    if (!allText.EndsWith("\n"))
                    {
                        if (lines.Length > 0)
                        {
                            // Keep the last incomplete line in buffer
                            _messageBuffer.Clear();
                            _messageBuffer.Append(lines[lines.Length - 1]);

                            // Process only complete lines
                            string[] completeLines = new string[lines.Length - 1];
                            Array.Copy(lines, completeLines, lines.Length - 1);
                            lines = completeLines;
                        }
                    }
                    else
                    {
                        _messageBuffer.Clear();
                    }

                    foreach (string line in lines)
                    {
                        AppendReceivedData(line + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Reception error: {ex.Message}");
            }
            finally
            {
                _networkStream?.Close();
                _tcpClient?.Close();
            }
        }

        /// <summary>
        /// Updates the status text on the UI thread.
        /// </summary>
        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() => StatusTextBlock.Text = message);
        }

        /// <summary>
        /// Appends received data to the text box and triggers the sensorDataArrived event.
        /// </summary>
        private void AppendReceivedData(string message)
        {
            sensorDataArrived?.Invoke(this, new SensorDataArrivedEventArgs(UpdateLine(message)));

            Dispatcher.Invoke(() =>
            {
                HeartRateTextBox.AppendText(message);
                HeartRateTextBox.ScrollToEnd();
            });
        }

        /// <summary>
        /// Parses a single line of incoming data and formats it for display.
        /// Supports Polar H10, Bangle heart rate, and Bangle accelerometer.
        /// </summary>
        public string UpdateLine(string line)
        {
            string result = "";

            // Parsing Polar H10 HR and RR intervals
            if (line.StartsWith("PolarH10:"))
            {
                var parts = line.Substring("PolarH10:".Length).Split('|');
                if (parts.Length >= 2)
                {
                    string message = "";

                    if (int.TryParse(parts[0].Replace("BPM", "").Trim(), out int bpm))
                        message += $"BPM: {bpm}; ";

                    var rrParts = parts[1].Split(',');
                    List<double> parsedRR = new List<double>();

                    foreach (var rr in rrParts)
                    {
                        string clean = rr.Replace("ms", "").Trim();
                        if (double.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rrVal))
                            parsedRR.Add(rrVal);
                    }

                    if (parsedRR.Count > 0)
                        message += $"RR: [{string.Join(", ", parsedRR)}]";

                    result = message;
                }
            }
            // Parsing Bangle heart rate
            else if (line.StartsWith("BangleHeart:"))
            {
                var parts = line.Substring("BangleHeart:".Length).Split('|');
                if (parts.Length >= 1 &&
                    int.TryParse(parts[0].Replace("BPM", "").Trim(), out int bpm))
                {
                    result = $"BPM: {bpm}";
                }
            }
            // Parsing throttled Bangle accelerometer
            else if (line.StartsWith("BanglAccelerometer:"))
            {
                var values = line.Substring("BanglAccelerometer:".Length).Split(',');

                if (values.Length == 3 &&
                    double.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double y) &&
                    double.TryParse(values[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double z))
                {
                    result = $"X={x}, Y={y}, Z={z}";
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Event arguments for sensor data arrival.
    /// </summary>
    public sealed class SensorDataArrivedEventArgs : EventArgs
    {
        public IDictionary<string, string> dictionary;

        public SensorDataArrivedEventArgs(string message)
        {
            dictionary = new Dictionary<string, string>
            {
                { "SensorData", message }
            };
        }
    }
}
