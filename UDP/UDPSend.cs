using Kinect_Middleware.Scripts;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;

namespace Kinect_Middleware.UDP {
    /// <summary>
    /// Sends information to Unity
    /// </summary>
    public class UDPSend {
        public BlockingCollection<string> messageQueue = new BlockingCollection<string>();
        public bool conected = true;

        private string ip = "127.0.0.1";
        private IPEndPoint remoteEndPoint;
        private UdpClient client;
        private Thread sendThread;

        private string strMessage;
        private bool disconecting = false;

        public int errorPackets = 0;

        public UDPSend() {
            int outPort = UDPPorts.outPort;
            int destinationPort = UDPPorts.destinationPort;

            try {
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), destinationPort);
                client = new UdpClient(outPort);
                try {


                    client.EnableBroadcast = true;
                } catch (SecurityException err) { }
            } catch (SocketException err) { }
        }

        public void sendPackets() {
            sendThread = new Thread(o => {
                while (conected) {
                    try {
                        strMessage = messageQueue.Take();
                    } catch (Exception err) {
                        if (disconecting) {
                            disconecting = false;
                        }
                    }

                    sendString(strMessage);
                }
            });

            try {
                sendThread.IsBackground = true;
                sendThread.Start();
            } catch (Exception err) { }
        }

        private int sendString(string message) {
            int bytesSended = 0;
            try {
                byte[] data = Encoding.UTF8.GetBytes(message);
                bytesSended = client.Send(data, data.Length, remoteEndPoint);
            } catch (Exception err) {
                errorPackets++;
            }

            return bytesSended;
        }

        public void SendGameSettings(string settings) {
            string dataHead = "#K2UMUDP";
            string dataFormat = "SS";
            string dataTypeSend = "CC";

            // Add the size of the packet
            int sizePackage = settings.Length;
            // The sending string is generated
            string packet = dataHead + dataTypeSend + sizePackage.ToString("D4") + dataFormat + settings;

            messageQueue.Add(packet);
        }

        public void disconnect(Thread sendThread) {
            disconecting = true;

            if (client != null) {
                try {
                    client.Close();
                } catch (SocketException err) { }
            }

            if (sendThread != null) {
                if (sendThread.IsAlive) {
                    try {
                        sendThread.Interrupt();
                        conected = false;
                    } catch (Exception err) { }
                }
            }
        }
    }
}