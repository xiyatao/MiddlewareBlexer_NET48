using Kinect_Middleware.Scripts;
using Kinect_Middleware.Scripts.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Kinect_Middleware.UDP
{
    /// <summary>
    /// Handles incoming UDP messages from Unity.
    /// Supports safe startup, reconnection, and graceful shutdown.
    /// </summary>
    public class UDPReceive
    {
        private Thread receiveThread;
        private UdpClient client;
        private int port;

        private bool disconnecting = false;
        private bool running = false;

        public int errorPackets = 0;
        public BlockingCollection<bool> messageWaiting = new BlockingCollection<bool>();

        // Optional auto-reconnect interval (ms)
        private const int ReconnectInterval = 5000;

        public UDPReceive()
        {
            this.port = UDPPorts.inPort;

            try
            {
                Console.WriteLine($"[UDPReceive] Initializing listener on port {port}...");
                StartReceiveThread();
                messageWaiting.Add(true);
            }
            catch (Exception err)
            {
                Console.WriteLine($"[UDPReceive] Initialization error: {err.Message}");
            }
        }

        /// <summary>
        /// Starts a background thread for receiving UDP packets.
        /// </summary>
        private void StartReceiveThread()
        {
            receiveThread = new Thread(new ThreadStart(ReceiveData))
            {
                IsBackground = true,
                Name = "UDPReceiveThread"
            };
            running = true;
            receiveThread.Start();
        }

        /// <summary>
        /// Main UDP reception loop.
        /// Handles reconnection, decoding, and user/game data dispatch.
        /// </summary>
        private void ReceiveData()
        {
            while (running)
            {
                try
                {
                    // Ensure client exists and is bound
                    if (client == null)
                    {
                        try
                        {
                            client = new UdpClient(port);
                            client.EnableBroadcast = true;
                            Console.WriteLine($"[UDPReceive] Listening on port {port}...");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[UDPReceive] Failed to bind port {port}: {ex.Message}");
                            Thread.Sleep(ReconnectInterval);
                            continue; // retry later
                        }
                    }

                    // Listen for incoming packets
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = client.Receive(ref anyIP);

                    if (bytes == null || bytes.Length == 0)
                        continue;

                    string asciiString = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    if (asciiString.Length < 8)
                        continue;

                    string header = asciiString.Substring(0, 8);

                    if (header == "#K2UMUDP")
                    {
                        string dataFormat = asciiString.Substring(14, 2);
                        var userManager = App.Host.Services.GetService<UserConfigurationManager>();

                        if (userManager != null && userManager.Authenticated)
                        {
                            string username = userManager.Username;

                            if (dataFormat == "SR")
                            {
                                string[] gameInfo = asciiString.Substring(16).Split('|');
                                GameSettingsManager generator = new GameSettingsManager(username, gameInfo);
                                var response = generator.SendSettings();
                                Console.WriteLine($"[UDPReceive] Received SR from {username}, userID={response.id_user}");
                            }
                            else if (dataFormat == "FR")
                            {
                                var manager = App.Host.Services.GetRequiredService<UserResultsManager>();
                                string results = asciiString.Substring(16);
                                manager.AddResult(results);
                                manager.GenerateResults(username, 0);
                                Console.WriteLine($"[UDPReceive] Received FR results from {username}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[UDPReceive] Warning: Unauthenticated user or missing service.");
                        }
                    }
                }
                catch (SocketException sockErr)
                {
                    errorPackets++;
                    Console.WriteLine($"[UDPReceive] Socket error ({errorPackets}): {sockErr.Message}");
                    Thread.Sleep(200); // avoid CPU overload
                }
                catch (Exception err)
                {
                    errorPackets++;
                    Console.WriteLine($"[UDPReceive] General error ({errorPackets}): {err.Message}");
                    Thread.Sleep(200);
                }
            }

            Console.WriteLine("[UDPReceive] Listener stopped.");
        }

        /// <summary>
        /// Gracefully stops the UDP listener and closes the socket.
        /// </summary>
        public void Disconnect()
        {
            disconnecting = true;
            running = false;

            try
            {
                client?.Close();
                client = null;
                Console.WriteLine("[UDPReceive] UDP client closed.");
            }
            catch (Exception err)
            {
                Console.WriteLine($"[UDPReceive] Error closing UDP client: {err.Message}");
            }

            try
            {
                if (receiveThread != null && receiveThread.IsAlive)
                {
                    receiveThread.Join(1000);
                    Console.WriteLine("[UDPReceive] Thread terminated cleanly.");
                }
            }
            catch (Exception err)
            {
                Console.WriteLine($"[UDPReceive] Error terminating thread: {err.Message}");
            }
        }
    }
}
