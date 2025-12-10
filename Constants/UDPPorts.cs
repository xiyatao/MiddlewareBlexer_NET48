/*
===================================================================================
 Author          : ---
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : ---
 Description     : Defines the UDP ports used for incoming, outgoing, and destination
                   communication in the application.
 Last Modified   : 2025-08-13
===================================================================================
*/

namespace Kinect_Middleware.Scripts
{
    /// <summary>
    /// Holds the UDP port numbers used for different types of communication.
    /// </summary>
    public class UDPPorts
    {
        // Port for incoming UDP messages
        public static int inPort = 8050;

        // Port used as the destination for outgoing UDP messages
        public static int destinationPort = 8051;

        // Port for outgoing UDP messages
        public static int outPort = 8052;
    }
}
