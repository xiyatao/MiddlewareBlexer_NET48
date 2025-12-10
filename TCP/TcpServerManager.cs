/*
===================================================================================
 Author          : Fedi Khayati
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : Master in Internet of Things (IoT) – 2024/2025
 Description     : Manages TCP server operations including client connections, message
                   reception, and broadcasting messages to all connected receivers.
 Last Modified   : 2025-08-13
===================================================================================
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kinect_Middleware.TCP
{
    /// <summary>
    /// Handles TCP server operations for clients designated as "senders" or "receivers".
    /// Senders can send messages to the server, which are broadcasted to all connected receivers.
    /// </summary>
    public class TcpServerManager
    {
        private TcpListener tcpListener;
        private bool listening;
        private readonly List<TcpClient> receivers = new List<TcpClient>();
        private readonly List<TcpClient> senders = new List<TcpClient>();
        private readonly object clientsLock = new object();

        /// <summary>
        /// Triggered when the TCP server status changes (e.g., started, error).
        /// </summary>
        public event Action<string> StatusUpdated;

        /// <summary>
        /// Triggered when a new client connects to the server.
        /// </summary>
        public event Action<string> ClientConnected;

        /// <summary>
        /// Triggered when a message is received from a sender or an error occurs.
        /// </summary>
        public event Action<string> MessageReceived;

        /// <summary>
        /// Starts the TCP server on a specific IP and port.
        /// Begins accepting client connections asynchronously.
        /// </summary>
        public void Start(string ip, int port)
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(ip), port);
                tcpListener.Start();
                listening = true;
                StatusUpdated?.Invoke($"TCP Server started on {ip}:{port}");

                Task.Run(() => AcceptClientsLoop());
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"TCP Server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Continuously accepts incoming client connections until the server is stopped.
        /// </summary>
        private void AcceptClientsLoop()
        {
            while (listening)
            {
                try
                {
                    var client = tcpListener.AcceptTcpClient();
                    ClientConnected?.Invoke("TCP Client connected");
                    Task.Run(() => HandleClient(client));
                }
                catch (SocketException)
                {
                    // Listener stopped; exit loop gracefully
                    break;
                }
                catch (Exception ex)
                {
                    StatusUpdated?.Invoke($"AcceptClientsLoop error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles an individual client, determines its type ("SENDER" or "RECEIVER"),
        /// and processes messages. Sender messages are broadcasted to all receivers.
        /// </summary>
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[1024];

                // Identify client type
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    client.Close();
                    return;
                }

                string clientType = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim().ToUpper();
                bool isSender = clientType.StartsWith("SENDER");
                bool isReceiver = clientType.Contains("RECEIVER");

                lock (clientsLock)
                {
                    if (isSender) senders.Add(client);
                    if (isReceiver) receivers.Add(client);

                    if (!isSender && !isReceiver)
                    {
                        client.Close(); // Unknown client type
                        return;
                    }
                }

                if (isSender)
                {
                    // Continuously read sender messages
                    while (true)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke($"Received from sender ({clientType}): {message.Trim()}");
                        BroadcastToReceivers(message);
                    }
                }
                else if (isReceiver)
                {
                    // Receiver-only clients remain connected
                    while (true)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageReceived?.Invoke($"Client connection error: {ex.Message}");
            }
            finally
            {
                MessageReceived?.Invoke("Client disconnected.");
                lock (clientsLock)
                {
                    senders.Remove(client);
                    receivers.Remove(client);
                }
                stream?.Close();
                client.Close();
            }
        }

        /// <summary>
        /// Broadcasts a message to all connected receivers, removing any disconnected ones.
        /// </summary>
        private void BroadcastToReceivers(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            List<TcpClient> disconnected = new List<TcpClient>();

            lock (clientsLock)
            {
                foreach (var receiver in receivers)
                {
                    try
                    {
                        if (receiver.Connected)
                        {
                            var stream = receiver.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                        else
                        {
                            disconnected.Add(receiver);
                        }
                    }
                    catch
                    {
                        disconnected.Add(receiver);
                    }
                }

                foreach (var d in disconnected)
                    receivers.Remove(d);
            }
        }

        /// <summary>
        /// Stops the TCP server and closes all client connections.
        /// </summary>
        public void Stop()
        {
            listening = false;
            tcpListener?.Stop();
        }
    }
}
