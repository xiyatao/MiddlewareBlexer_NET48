/*
===================================================================================
 Author          : Xiya Tao
 Institution     : Universidad Politécnica de Madrid (UPM)
 Description     : Updated main UI control for managing Kinect and Bluetooth sensors.
                   - Adds periodic Bluetooth status polling via DispatcherTimer
                   - Supports multi-device heart rate connection and disconnection
                   - Includes real-time TCP server event feedback (status, client, message)
                   - Provides colored UI indicators and grouped control enabling
                   - Integrates unified shutdown of all sensors on window closing
                   - Improves async handling for concurrent sensor operations
 Last Modified   : 2025-11-03
===================================================================================
*/

using Kinect_Middleware.Bluetooth;
using Kinect_Middleware.Constants;
using Kinect_Middleware.Kinect;
using Kinect_Middleware.Sensors;
using Kinect_Middleware.TCP;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Kinect_Middleware.Views.Pages
{
    /// <summary>
    /// MainViewNew is the central UI control for managing connected sensors and devices,
    /// such as Kinect, heart rate monitors, and accelerometers. It also provides real-time
    /// feedback on Bluetooth and TCP server status.
    /// </summary>
    public partial class MainViewNew : UserControl
    {
        private UniversalKinect universalKinect;
        private DispatcherTimer bluetoothStatusTimer;
        private BluetoothManager bluetoothManager;
        private TcpServerManager tcpServerManager;
        private SensorManager sensorManager;

        private bool isHeartRateConnected = false;
        private bool isAccelerometerConnected = false;

        public MainViewNew()
        {
            InitializeComponent();

            // Window closing listener
            Loaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                    window.Closing += Window_Closing;
            };

            universalKinect = App.Host.Services.GetRequiredService<UniversalKinect>();

            // Default selections
            ComboBox.SelectedIndex = 0;
            DataContext = universalKinect.Bindings;

            // Initialize Bluetooth manager
            bluetoothManager = new BluetoothManager();
            bluetoothManager.BluetoothStatusChanged += BluetoothManager_BluetoothStatusChanged;

            // Periodic Bluetooth status check
            bluetoothStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            bluetoothStatusTimer.Tick += (s, e) =>
            {
                bluetoothManager.CheckBluetoothState();
            };
            bluetoothStatusTimer.Start();

            // Initialize TCP server
            tcpServerManager = new TcpServerManager();
            tcpServerManager.StatusUpdated += TcpServerManager_StatusUpdated;
            tcpServerManager.ClientConnected += TcpServerManager_ClientConnected;
            tcpServerManager.MessageReceived += TcpServerManager_MessageReceived;
            tcpServerManager.Start(TCPConstants.TcpServerIp, TCPConstants.TcpPort);

            // Initialize sensor manager
            sensorManager = new SensorManager();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sensorManager.ShutdownAllSensors();
        }

        #region TCP & Bluetooth Events

        private void TcpServerManager_StatusUpdated(string status)
        {
            Dispatcher.Invoke(() =>
            {
                TcpStatusText.Text = status;
                TcpStatusText.Foreground = status.StartsWith("TCP Server started")
                    ? new SolidColorBrush(Colors.Green)
                    : new SolidColorBrush(Colors.Red);
            });
        }

        private void TcpServerManager_ClientConnected(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TcpClientText.Text = message;
                TcpClientText.Foreground = new SolidColorBrush(Colors.LightGreen);
            });
        }

        private void TcpServerManager_MessageReceived(string message)
        {
            Console.WriteLine($"[TCP] {message}");
        }

        private void BluetoothManager_BluetoothStatusChanged(bool isOn, string status)
        {
            Dispatcher.Invoke(() =>
            {
                BluetoothStatusText.Text = status;
                BluetoothStatusText.Foreground = isOn
                    ? new SolidColorBrush(Colors.Green)
                    : new SolidColorBrush(Colors.Red);
                SensorSelectionGroupBox.IsEnabled = isOn;

                if (!isOn)
                {
                    AccelerometerCheckBox.IsChecked = false;
                    HeartRateCheckBox.IsChecked = false;
                }
            });
        }

        #endregion

        #region Kinect Controls

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected = (ComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selected == "Azure Kinect")
                universalKinect.selectedKinect = KinectType.Azure;
            else if (selected == "Xbox one Kinect")
                universalKinect.selectedKinect = KinectType.One;
        }

        private void Start_Click(object sender, RoutedEventArgs e) => universalKinect.start();
        private void Stop_Click(object sender, RoutedEventArgs e) => universalKinect.stop();

        #endregion

        #region Sensor Checkboxes

        private void AccelerometerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AccelerometerSensorControls.Visibility = Visibility.Visible;
        }

        private void AccelerometerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isAccelerometerConnected)
            {
                AccelerometerCheckBox.IsChecked = true;
                return;
            }
            AccelerometerSensorControls.Visibility = Visibility.Collapsed;
        }

        private void HeartRateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HeartRateSensorControls.Visibility = Visibility.Visible;
        }

        private void HeartRateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isHeartRateConnected)
            {
                HeartRateCheckBox.IsChecked = true;
                return;
            }
            HeartRateSensorControls.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Heart Rate (Multi-Device Logic)

        private async void HeartRateConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HeartRateSensorListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select at least one heart rate sensor.",
                                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var item in HeartRateSensorListBox.SelectedItems)
                {
                    string sensorName = ((ListBoxItem)item).Content.ToString();
                    Console.WriteLine($"[HeartRate] Connecting {sensorName}...");
                    await sensorManager.ConnectHeartRateSensor(sensorName);
                }

                HeartRateDisconnect.IsEnabled = true;
                isHeartRateConnected = true;
                universalKinect.Bindings.IsHeartRateRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect sensors: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HeartRateDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HeartRateSensorListBox.SelectedItems.Count > 0)
                {
                    foreach (var item in HeartRateSensorListBox.SelectedItems)
                    {
                        string sensorName = ((ListBoxItem)item).Content.ToString();
                        Console.WriteLine($"[HeartRate] Disconnecting {sensorName}...");
                        sensorManager.DisconnectHeartRateSensor(sensorName);
                    }
                }
                else
                {
                    sensorManager.DisconnectHeartRateSensor("Polar H10");
                    sensorManager.DisconnectHeartRateSensor("Bangle watch");
                }

                HeartRateDisconnect.IsEnabled = false;
                isHeartRateConnected = false;
                universalKinect.Bindings.IsHeartRateRunning = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to disconnect sensors: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Accelerometer

        private async void AccelerometerConnect_Click(object sender, RoutedEventArgs e)
        {
            string selected = (AccelerometerSensorComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(selected))
            {
                MessageBox.Show("Please select a valid accelerometer sensor.",
                                "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AccelerometerConnect.IsEnabled = false;
            await sensorManager.ConnectAccelerometerSensor(selected);

            isAccelerometerConnected = true;
            AccelerometerDisconnect.IsEnabled = true;
            universalKinect.Bindings.IsAccelerometerRunning = true;
        }

        private void AccelerometerDisconnect_Click(object sender, RoutedEventArgs e)
        {
            string selected = (AccelerometerSensorComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(selected)) return;

            sensorManager.DisconnectAccelerometerSensor(selected);

            isAccelerometerConnected = false;
            AccelerometerConnect.IsEnabled = true;
            AccelerometerDisconnect.IsEnabled = false;
            universalKinect.Bindings.IsAccelerometerRunning = false;
        }

        #endregion
    }
}
