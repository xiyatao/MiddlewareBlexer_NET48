/*
===================================================================================
 Author          : Fedi Khayati
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : Master in Internet of Things (IoT) – 2024/2025
 Description     : Monitors the Bluetooth radio status, detects state changes, and 
                   reports Bluetooth availability and connected devices.
 Last Modified   : 2025-06-23
===================================================================================
*/

using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;

namespace Kinect_Middleware.Bluetooth
{
    /// <summary>
    /// Handles monitoring of Bluetooth radio status using the InTheHand.Net library.
    /// Checks the primary Bluetooth adapter's state and notifies subscribers when it changes.
    /// </summary>
    public class BluetoothManager
    {
        // Tracks the last known Bluetooth radio mode to detect state changes
        private RadioMode? lastRadioMode = null;

        /// <summary>
        /// Event triggered whenever the Bluetooth status changes.
        /// Parameters:
        ///  - bool: true if Bluetooth is enabled, false otherwise
        ///  - string: descriptive message indicating current status
        /// </summary>
        public event Action<bool, string> BluetoothStatusChanged;

        /// <summary>
        /// Checks the current state of the primary Bluetooth radio and triggers status events.
        /// Detects if Bluetooth is off, on, unavailable, or if any devices are connected.
        /// </summary>
        public void CheckBluetoothState()
        {
            try
            {
                // Retrieve the primary Bluetooth radio (adapter)
                var radio = BluetoothRadio.PrimaryRadio;

                // If no radio is detected, consider Bluetooth as off or unavailable
                if (radio == null)
                {
                    lastRadioMode = null;
                    BluetoothStatusChanged?.Invoke(false, "Bluetooth is OFF or unavailable");
                    return;
                }

                var currentMode = radio.Mode;

                // If the radio mode hasn't changed since the last check, no further action is needed
                if (lastRadioMode == currentMode)
                {
                    return;
                }

                lastRadioMode = currentMode;

                // If Bluetooth is powered off, notify subscribers
                if (currentMode == RadioMode.PowerOff)
                {
                    BluetoothStatusChanged?.Invoke(false, "Bluetooth is OFF");
                    return;
                }

                // If Bluetooth is on, attempt to discover nearby devices
                using (var client = new BluetoothClient())
                {
                    var devices = client.DiscoverDevices();

                    if (devices.Length > 0)
                        BluetoothStatusChanged?.Invoke(true, "Bluetooth is ON");
                    else
                        BluetoothStatusChanged?.Invoke(true, "Bluetooth is ON (no devices found)");
                }
            }
            catch (Exception ex)
            {
                // Notify subscribers if an error occurs during the Bluetooth check
                BluetoothStatusChanged?.Invoke(false, $"Bluetooth check failed: {ex.Message}");
            }
        }
    }
}
