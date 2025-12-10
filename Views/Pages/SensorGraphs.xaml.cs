/*
===================================================================================
 Author          : Fedi Khayati
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : Master in Internet of Things (IoT) – 2024/2025
 Description     : Displays real-time sensor data in graphical charts:
                   - Heart rate (BPM)
                   - RR intervals
                   - Accelerometer (X, Y, Z)
                   Handles TCP connection to the middleware and throttles
                   accelerometer updates for performance.
 Last Modified   : 2025-08-13
===================================================================================
*/

using Kinect_Middleware.Constants;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Kinect_Middleware.Views.Pages
{
    /// <summary>
    /// SensorGraphs is a WPF UserControl that plots live heart rate, RR interval, and accelerometer data
    /// using LiveCharts. It connects as a TCP receiver to the middleware to receive sensor data.
    /// </summary>
    public partial class SensorGraphs : UserControl
    {
        // Chart collections
        public SeriesCollection AccelSeries { get; set; }
        public SeriesCollection HeartRateSeries { get; set; }
        public SeriesCollection RRSeries { get; set; }

        // Individual chart values
        public ChartValues<double> AccelX { get; set; } = new ChartValues<double>();
        public ChartValues<double> AccelY { get; set; } = new ChartValues<double>();
        public ChartValues<double> AccelZ { get; set; } = new ChartValues<double>();
        public ChartValues<int> HeartRateValues { get; set; } = new ChartValues<int>();
        public ChartValues<double> RRValues { get; set; } = new ChartValues<double>();

        private DateTime _lastAccelUpdate = DateTime.MinValue;
        private readonly TimeSpan _accelThrottleInterval = TimeSpan.FromMilliseconds(500);

        // TCP client
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private StringBuilder _buffer = new StringBuilder();

        public SensorGraphs()
        {
            InitializeComponent();
            DataContext = this;
            InitChartSeries();
            _ = ConnectToTcpServerAsync();
        }

        private void InitChartSeries()
        {
            // Accelerometer series
            AccelSeries = new SeriesCollection
            {
                new LineSeries { Title = "X", Values = AccelX },
                new LineSeries { Title = "Y", Values = AccelY },
                new LineSeries { Title = "Z", Values = AccelZ }
            };

            // Heart rate series
            HeartRateSeries = new SeriesCollection
            {
                new LineSeries { Title = "BPM", Values = HeartRateValues }
            };

            // RR interval series
            RRSeries = new SeriesCollection
            {
                new LineSeries { Title = "RR (ms)", Values = RRValues }
            };
        }

        private async Task ConnectToTcpServerAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(TCPConstants.TcpServerIp, TCPConstants.TcpPort);
                _stream = _tcpClient.GetStream();

                // Identify as receiver
                byte[] receiverId = Encoding.UTF8.GetBytes("RECEIVER\n");
                await _stream.WriteAsync(receiverId, 0, receiverId.Length);

                await ReadDataLoopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TCP Connection Error: {ex.Message}");
            }
        }

        private async Task ReadDataLoopAsync()
        {
            byte[] buffer = new byte[TCPConstants.BufferSize];

            try
            {
                while (true)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);
                    _buffer.Append(chunk);

                    while (_buffer.ToString().Contains('\n'))
                    {
                        int index = _buffer.ToString().IndexOf('\n');
                        string line = _buffer.ToString().Substring(0, index).Trim();
                        _buffer.Remove(0, index + 1);

                        Dispatcher.Invoke(() => UpdateWithTcpLine(line));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TCP Read Error: {ex.Message}");
            }
        }

        private void UpdateWithTcpLine(string line)
        {
            Debug.WriteLine($"Received: {line}");

            // Polar H10
            if (line.StartsWith("PolarH10:"))
            {
                var parts = line.Substring("PolarH10:".Length).Split('|');
                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[0].Replace("BPM", "").Trim(), out int bpm))
                        HeartRateValues.Add(bpm);

                    var rrParts = parts[1].Split(',');
                    foreach (var rr in rrParts)
                    {
                        string clean = rr.Replace("ms", "").Trim();
                        if (double.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rrVal))
                            RRValues.Add(rrVal);
                    }
                }
            }
            // Bangle HR
            else if (line.StartsWith("BangleHeart:"))
            {
                var parts = line.Substring("BangleHeart:".Length).Split('|');
                if (parts.Length >= 1 && int.TryParse(parts[0].Replace("BPM", "").Trim(), out int bpm))
                    HeartRateValues.Add(bpm);
            }
            // Throttled accelerometer
            else if (line.StartsWith("BanglAccelerometer:"))
            {
                var now = DateTime.Now;
                if ((now - _lastAccelUpdate) < _accelThrottleInterval) return;

                _lastAccelUpdate = now;
                var values = line.Substring("BanglAccelerometer:".Length).Split(',');

                if (values.Length == 3 &&
                    double.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double y) &&
                    double.TryParse(values[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double z))
                {
                    AccelX.Add(x);
                    AccelY.Add(y);
                    AccelZ.Add(z);
                }
            }

            // Keep chart size manageable
            LimitPoints(HeartRateValues, 100);
            LimitPoints(RRValues, 100);
            LimitPoints(AccelX, 100);
            LimitPoints(AccelY, 100);
            LimitPoints(AccelZ, 100);
        }

        private void LimitPoints<T>(ChartValues<T> values, int max) where T : struct
        {
            while (values.Count > max)
                values.RemoveAt(0);
        }
    }
}
