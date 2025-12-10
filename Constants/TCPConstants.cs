/*
===================================================================================
 Author          : ---
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : ---
 Description     : Defines TCP-related constants used for server-client communication,
                   including port, buffer size, and server IP.
 Last Modified   : 2025-08-13
===================================================================================
*/

namespace Kinect_Middleware.Constants
{
    /// <summary>
    /// Contains constants for TCP communication, such as port number, buffer size, and server IP.
    /// </summary>
    internal class TCPConstants
    {
        // TCP port used by the server
        public const int TcpPort = 12345;

        // Size of the buffer used for sending/receiving data
        public const int BufferSize = 1024;

        // IP address of the TCP server; listens only on localhost
        public const string TcpServerIp = "127.0.0.1";
    }
}
